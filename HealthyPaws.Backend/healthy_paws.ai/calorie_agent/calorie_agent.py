import math
from dataclasses import replace
from typing import Optional

from calorie_agent.config.config import CalorieAgentConfig
from calorie_agent.core.calculations import Calculations
from calorie_agent.core.classification.body_state import BodyState
from calorie_agent.core.classification.classify import Classify

from calorie_agent.core.decisions import Decisions
from calorie_agent.core.estimator import Estimator
from calorie_agent.core.initial_plan import InitialPlan
from calorie_agent.core.regulator import Regulator
from calorie_agent.core.trend import Trend, fit
from calorie_agent.interfaces.action import Action
from calorie_agent.interfaces.learned_state import LearnedState
from calorie_agent.interfaces.tick_request import TickRequest
from calorie_agent.interfaces.week_record import WeekRecord
from calorie_agent.validators.severity import Severity
from calorie_agent.validators.tick_request_validator import TickRequestValidator
from rpc.service.calorie_agent.calorie_agent_pb2 import CalorieAgentRequest, CalorieAgentResponse


class CalorieAgent:
    def __init__(self, cfg: CalorieAgentConfig):
        self.learned : LearnedState
        self.cfg = cfg
        self.decisions = Decisions(self)
        self.calculations = Calculations(self.cfg)
        self.estimator = Estimator(self)
        self.classify = Classify(self)
        self.reasons: list[str] = []


    def run(self, req: CalorieAgentRequest) -> CalorieAgentResponse:
        self._init_request(req)

        validation_result = self._validate()
        if validation_result is not None:
            return validation_result

        self._calculate_and_assign_prior()

        self.classify.classify_and_check()

        if self.plan is None or self.plan.current_daily_kcal is None:
            initial_plan = InitialPlan(self)
            res = initial_plan.create_initial_plan()
            return self.decisions.finish(Action.SET_INITIAL, kcal=res)
        if self.prev is None:
            return self.decisions.stop(Action.NO_CHANGE, "FIRST_MEASUREMENT")

        intake = self._check_intake()
        if intake is not None:
            return intake

        learn = self._learn()
        if learn is not None:
            return learn
        
        safety = self._check_safety()
        if safety is not None:
            return safety
        
        ideal = self._is_ideal()
        if ideal is not None:
            return ideal

        return self._regulate()

    def _init_request(self, req: CalorieAgentRequest) -> None:
        self.request = TickRequest.serialize(req)
        self.m, self.profile, self.plan = self.request.measurement, self.request.profile, self.request.plan
        self.learned: LearnedState = LearnedState.sane_learned(self)
        self.ctx: dict = {"weight": self.m.weight, "bcs": self.m.bcs}
        self.diag: dict = {}
        self.hist: list[WeekRecord] = sorted(
            (r for r in self.request.history if math.isfinite(r.weight_kg) and r.weight_kg > 0),
            key=lambda r: r.measured_at,
        )
        self.prev: Optional[WeekRecord] = self.hist[-1] if self.hist else None
        self.dt_days: Optional[float] = (
            (self.m.measured_at - self.prev.measured_at).total_seconds() / 86400.0 if self.prev else None
        )
        self.sigma_w = self.calculations.weight_sigma(self.m.weight) if self.m.weight > 0 else self.cfg.weight_sigma_abs_kg

    def _validate(self) -> CalorieAgentResponse | None:
        issues = TickRequestValidator.validate(self.request, self.cfg)
        if issues:
            self.diag["validation"] = [i.to_dict() for i in issues]
        block = TickRequestValidator.first_blocking(issues)
        if block is not None:
            self.ctx["detail"] = block.detail
            if block.severity is Severity.REMEASURE:
                if block.confirmable:
                    return self.decisions.confirm_or_remeasure()
                return self.decisions.stop(Action.REQUEST_REMEASURE, "INVALID_MEASUREMENT")
            return self.decisions.stop(Action.ESCALATE, "INVALID_INPUT")

        if self.profile.life_stage.strip().lower() == "puppy" or self.profile.pregnant_or_lactating:
            return self.decisions.stop(Action.ESCALATE, "UNSUPPORTED_LIFE_STAGE")

        if self.prev is not None and self.m.measured_at <= self.prev.measured_at:
            return self.decisions.stop(Action.NO_CHANGE, "STALE_MEASUREMENT")

        if self.dt_days is not None and self.dt_days < self.cfg.min_period_days:
            self.ctx["dt_days"] = self.dt_days
            return self.decisions.stop(Action.NO_CHANGE, "MEASUREMENT_TOO_SOON")

        early = self.decisions.plausibility()
        if early is not None:
            return early

        return None

    def _calculate_and_assign_prior(self):
        prior = self.calculations.mer_prior(self.m.weight, self.profile)
        self.ctx["prior_mer"] = prior
        if self.learned.mer_estimate_kcal is None or self.learned.variance_kcal2 is None:
            self.learned = self.estimator.init_learned(prior, self.learned)

    def _check_intake(self) -> CalorieAgentResponse | None:
        it = self.request.intake
        if it is None:
            return None

        coverage = it.days_covered / it.period_days if it.period_days > 0 else 0.0
        if it.eaten_g_per_day is None or coverage < self.cfg.min_intake_coverage:
            return self.decisions.stop(Action.NO_CHANGE, "NO_INTAKE_DATA")

        if it.offered_g_per_day and it.offered_g_per_day > 0:
            ratio = it.eaten_g_per_day / it.offered_g_per_day
            self.diag["eaten_ratio"] = ratio
            if ratio < self.cfg.appetite_ratio_threshold:
                self.ctx["eaten_ratio"] = ratio
                return self.decisions.stop(Action.ESCALATE, "REDUCED_APPETITE")

        eaten_kcal = it.eaten_g_per_day * self.profile.food_kcal_per_gram
        self.ctx["eaten_kcal"] = eaten_kcal
        verdict = TickRequestValidator.intake_problem(eaten_kcal, self.calculations.rer(self.m.weight))
        if verdict is not None:
            return self.decisions.stop(Action.ESCALATE if verdict is Severity.ESCALATE else Action.NO_CHANGE, "INTAKE_IMPLAUSIBLE")
        return None

    def _learn(self) -> CalorieAgentResponse | None:
        assert self.prev is not None and self.dt_days is not None
        inn = self.estimator.innovation(self.learned, self.ctx["eaten_kcal"], self.prev.weight_kg, self.m.weight,
                                   self.dt_days, self.sigma_w)
        self.diag.update(innovation_kcal=inn.innovation, innovation_z=inn.z,
                         predicted_dw_kg=inn.predicted_dw_kg, actual_dw_kg=inn.actual_dw_kg,
                         observed_mer=inn.observed_mer)
        self.ctx["z"] = inn.z
        if abs(inn.z) > self.cfg.innovation_z_threshold:
            bad = self.learned.consecutive_bad_weeks + 1
            self.learned = replace(self.learned, consecutive_bad_weeks=bad)
            if bad >= self.cfg.bad_weeks_before_escalation:
                return self.decisions.stop(Action.ESCALATE, "MODEL_MISMATCH_PERSISTENT")
            return self.decisions.stop(Action.NO_CHANGE, "MODEL_ERROR_LARGE")
        self.learned = self.estimator.apply_update(self.learned, inn, self.m.measured_at)
        mer_est = self.learned.mer_estimate_kcal
        assert mer_est is not None
        self.diag.update(mer_estimate_kcal=mer_est, mer_sd_kcal=math.sqrt(self.learned.variance_kcal2 or 0.0))
        self.ctx["mer_est"] = mer_est
        return None

    def _check_safety(self) -> CalorieAgentResponse | None:
        mer_est = self.ctx["mer_est"]
        prior = self.ctx["prior_mer"]
        cls = self.ctx["cls"]

        if abs(mer_est / prior - 1.0) > self.cfg.prior_deviation_max:
            return self.decisions.stop(Action.ESCALATE, "ESTIMATE_FAR_FROM_PRIOR")

        rows = [r.weight_kg for r in self.hist if r.bcs is None or r.bcs < self.cfg.bcs_high_min]
        if len(rows) >= self.cfg.window_min_rows:
            old = rows[: max(2, len(rows) - 1) // 2 + 1]
            baseline = sum(old) / len(old)
            recent_pts = [rows[-1], self.m.weight]
            recent = sum(recent_pts) / len(recent_pts)
            noise_kg = self.sigma_w * math.sqrt(1.0 / len(old) + 1.0 / len(recent_pts))
            loss_kg = baseline - recent
            pct = self.cfg.window_loss_pct_on_diet if cls.state is BodyState.OVERWEIGHT else self.cfg.window_loss_pct
            limit_kg = max(pct / 100.0 * baseline, self.cfg.window_alarm_sigma * noise_kg)
            self.diag.update(window_loss_kg=loss_kg, window_limit_kg=limit_kg)
            if loss_kg > limit_kg:
                self.ctx["slope"] = -loss_kg
                return self.decisions.stop(Action.ESCALATE,
                                  "RAPID_LOSS_ON_DIET" if cls.state is BodyState.OVERWEIGHT else "UNEXPLAINED_WEIGHT_LOSS")

        trend = self._trend()
        stable = True
        self.ctx["stable"] = stable

        if trend is not None:
            self.diag.update(slope_kg_per_week=trend.slope_kg_per_week, slope_se=trend.se_kg_per_week)
            self.ctx["slope"] = trend.slope_kg_per_week
            stable = abs(trend.slope_kg_per_week) <= max(
                self.cfg.stable_slope_pct_per_week / 100.0 * self.m.weight, self.cfg.stable_sigma * trend.se_kg_per_week)
            pct = self.cfg.rapid_loss_pct_per_week if cls.state is BodyState.OVERWEIGHT else self.cfg.unexplained_loss_pct_per_week

            self.ctx.update(stable=stable)

            if trend.slope_kg_per_week < -(pct / 100.0 * self.m.weight + self.cfg.trend_alarm_sigma * trend.se_kg_per_week):
                code = "RAPID_LOSS_ON_DIET" if cls.state is BodyState.OVERWEIGHT else "UNEXPLAINED_WEIGHT_LOSS"
                return self.decisions.stop(Action.ESCALATE, code)

        return None

    def _was_not_ideal(self) -> bool:
        prev = self.prev
        if prev is None:
            return False
        return self.calculations.bcs_direction(prev.bcs) != 0

    def _trend(self) -> Optional[Trend]:
        m, cfg = self.m, self.cfg
        recent = self.hist[-(cfg.trend_points - 1):]
        pts = [((r.measured_at - m.measured_at).total_seconds() / (7 * 86400.0), r.weight_kg) for r in recent]
        pts.append((0.0, m.weight))
        return fit(pts, self.sigma_w)

    def _is_ideal(self):
        cls = self.ctx["cls"]
        stable = self.ctx["stable"]
        if cls.state is BodyState.IDEAL:
            arrived = self._was_not_ideal()
            self.diag["arrived_at_target"] = arrived
            if stable and not arrived:
                self.learned = replace(self.learned, weeks_at_bound=0)
                return self.decisions.stop(Action.NO_CHANGE, "IN_DEAD_ZONE")
            if arrived:
                self.reasons.append("TARGET_REACHED")
        return None

    def _regulate(self):
        regulator = Regulator(self)
        return regulator.regulate()

