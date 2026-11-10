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
    SECONDS_PER_DAY = 86400.0
    SECONDS_PER_WEEK = 7 * SECONDS_PER_DAY

    def __init__(self, config: CalorieAgentConfig):
        self.learned: LearnedState
        self.config = config
        self.decisions = Decisions(self)
        self.calculations = Calculations(self.config)
        self.estimator = Estimator(self)
        self.classifier = Classify(self)
        self.reason_codes: list[str] = []

    def run(self, proto_request: CalorieAgentRequest) -> CalorieAgentResponse:
        """Lanac čuvara: svaki korak vraća gotov odgovor (obrada staje) ili None (nastavlja se dalje)."""
        self._init_request(proto_request)

        validation_response = self._validate()
        if validation_response is not None:
            return validation_response

        self._calculate_and_assign_prior()

        classification_response = self.classifier.classify_and_check()
        if classification_response is not None:
            return classification_response

        if self.plan is None or self.plan.current_daily_kcal is None:
            initial_daily_kcal = InitialPlan(self).create_initial_plan()
            return self.decisions.finish(Action.SET_INITIAL, kcal=initial_daily_kcal)
        if self.previous_record is None:
            return self.decisions.stop(Action.NO_CHANGE, "FIRST_MEASUREMENT")

        intake_response = self._check_intake()
        if intake_response is not None:
            return intake_response

        learning_response = self._learn()
        if learning_response is not None:
            return learning_response

        safety_response = self._check_safety()
        if safety_response is not None:
            return safety_response

        ideal_response = self._is_ideal()
        if ideal_response is not None:
            return ideal_response

        return self._regulate()

    def _init_request(self, proto_request: CalorieAgentRequest) -> None:
        self.request = TickRequest.serialize(proto_request)
        self.measurement = self.request.measurement
        self.profile = self.request.profile
        self.plan = self.request.plan
        self.learned: LearnedState = LearnedState.sane_learned(self)
        self.context: dict = {"weight": self.measurement.weight, "bcs": self.measurement.bcs}
        self.diagnostics: dict = {}
        # Historija bez nevaljanih težina, od najstarijeg mjerenja prema najnovijem.
        self.history: list[WeekRecord] = sorted(
            (record for record in self.request.history if math.isfinite(record.weight_kg) and record.weight_kg > 0),
            key=lambda record: record.measured_at,
        )
        self.previous_record: Optional[WeekRecord] = self.history[-1] if self.history else None
        self.days_since_previous: Optional[float] = (
            (self.measurement.measured_at - self.previous_record.measured_at).total_seconds() / self.SECONDS_PER_DAY
            if self.previous_record else None
        )
        self.weight_sigma_kg = (self.calculations.weight_sigma(self.measurement.weight)
                                if self.measurement.weight > 0 else self.config.weight_sigma_abs_kg)

    def _validate(self) -> CalorieAgentResponse | None:
        issues = TickRequestValidator.validate(self.request, self.config)
        if issues:
            self.diagnostics["validation"] = [issue.to_dict() for issue in issues]
        blocking_issue = TickRequestValidator.first_blocking(issues)
        if blocking_issue is not None:
            self.context["detail"] = blocking_issue.detail
            if blocking_issue.severity is Severity.REMEASURE:
                if blocking_issue.confirmable:
                    return self.decisions.confirm_or_remeasure()
                return self.decisions.stop(Action.REQUEST_REMEASURE, "INVALID_MEASUREMENT")
            return self.decisions.stop(Action.ESCALATE, "INVALID_INPUT")

        if self.profile.life_stage.strip().lower() == "puppy" or self.profile.pregnant_or_lactating:
            return self.decisions.stop(Action.ESCALATE, "UNSUPPORTED_LIFE_STAGE")

        if self.previous_record is not None and self.measurement.measured_at <= self.previous_record.measured_at:
            return self.decisions.stop(Action.NO_CHANGE, "STALE_MEASUREMENT")

        if self.days_since_previous is not None and self.days_since_previous < self.config.min_period_days:
            self.context["days_since_previous"] = self.days_since_previous
            return self.decisions.stop(Action.NO_CHANGE, "MEASUREMENT_TOO_SOON")

        return self.decisions.plausibility()

    def _calculate_and_assign_prior(self) -> None:
        """Prior = procjena potrošnje iz formule, prije nego što agent išta nauči o ovom psu."""
        prior_mer = self.calculations.mer_prior(self.measurement.weight, self.profile)
        self.context["prior_mer"] = prior_mer
        if self.learned.mer_estimate_kcal is None or self.learned.variance_kcal2 is None:
            self.learned = self.estimator.init_learned(prior_mer, self.learned)

    def _check_intake(self) -> CalorieAgentResponse | None:
        intake = self.request.intake
        if intake is None:
            return self.decisions.stop(Action.NO_CHANGE, "NO_INTAKE_DATA")

        intake_coverage = intake.days_covered / intake.period_days if intake.period_days > 0 else 0.0
        if intake.eaten_g_per_day is None or intake_coverage < self.config.min_intake_coverage:
            return self.decisions.stop(Action.NO_CHANGE, "NO_INTAKE_DATA")

        if intake.offered_g_per_day and intake.offered_g_per_day > 0:
            eaten_to_offered_ratio = intake.eaten_g_per_day / intake.offered_g_per_day
            self.diagnostics["eaten_ratio"] = eaten_to_offered_ratio
            if eaten_to_offered_ratio < self.config.appetite_ratio_threshold:
                self.context["eaten_ratio"] = eaten_to_offered_ratio
                return self.decisions.stop(Action.ESCALATE, "REDUCED_APPETITE")

        feeder_kcal = intake.eaten_g_per_day * self.profile.food_kcal_per_gram
        self.context["eaten_kcal"] = feeder_kcal
        intake_severity = TickRequestValidator.intake_problem(
            feeder_kcal, self.calculations.rer(self.measurement.weight), self.config)
        if intake_severity is not None:
            action = Action.ESCALATE if intake_severity is Severity.ESCALATE else Action.NO_CHANGE
            return self.decisions.stop(action, "INTAKE_IMPLAUSIBLE")

        # Poslastice se računaju samo u energetski bilans (učenje), ne u provjere hranilice/apetita.
        has_valid_treats = (intake.treat_kcal_per_day is not None
                            and math.isfinite(intake.treat_kcal_per_day) and intake.treat_kcal_per_day > 0)
        treat_kcal = intake.treat_kcal_per_day if has_valid_treats else 0.0
        self.context["eaten_kcal"] = feeder_kcal + treat_kcal
        self.diagnostics.update(feeder_kcal=feeder_kcal, treat_kcal=treat_kcal)
        return None

    def _learn(self) -> CalorieAgentResponse | None:
        assert self.previous_record is not None and self.days_since_previous is not None
        innovation = self.estimator.innovation(
            self.learned, self.context["eaten_kcal"], self.previous_record.weight_kg, self.measurement.weight,
            self.days_since_previous, self.weight_sigma_kg)
        self.diagnostics.update(
            innovation_kcal=innovation.innovation_kcal, innovation_z=innovation.z_score,
            predicted_dw_kg=innovation.predicted_weight_change_kg, actual_dw_kg=innovation.actual_weight_change_kg,
            observed_mer=innovation.observed_mer)
        self.context["z"] = innovation.z_score

        if abs(innovation.z_score) > self.config.innovation_z_threshold:
            # "Loša sedmica": mjerenje je statistički nevjerovatno, pa se ne ugrađuje u procjenu.
            consecutive_bad_weeks = self.learned.consecutive_bad_weeks + 1
            self.learned = replace(self.learned, consecutive_bad_weeks=consecutive_bad_weeks)
            if consecutive_bad_weeks >= self.config.bad_weeks_before_escalation:
                return self.decisions.stop(Action.ESCALATE, "MODEL_MISMATCH_PERSISTENT")
            return self.decisions.stop(Action.NO_CHANGE, "MODEL_ERROR_LARGE")

        self.learned = self.estimator.apply_update(self.learned, innovation, self.measurement.measured_at)
        mer_estimate = self.learned.mer_estimate_kcal
        assert mer_estimate is not None
        self.diagnostics.update(mer_estimate_kcal=mer_estimate,
                                mer_sd_kcal=math.sqrt(self.learned.variance_kcal2 or 0.0))
        self.context["mer_estimate"] = mer_estimate
        return None

    def _check_safety(self) -> CalorieAgentResponse | None:
        mer_estimate = self.context["mer_estimate"]
        prior_mer = self.context["prior_mer"]
        is_overweight = self.context["classification"].state is BodyState.OVERWEIGHT
        weight_loss_code = "RAPID_LOSS_ON_DIET" if is_overweight else "UNEXPLAINED_WEIGHT_LOSS"

        if abs(mer_estimate / prior_mer - 1.0) > self.config.prior_deviation_max:
            return self.decisions.stop(Action.ESCALATE, "ESTIMATE_FAR_FROM_PRIOR")

        # Prozor historije: ukupan gubitak preko više sedmica je otporniji na šum od nagiba.
        # Redovi iz perioda debljine (visok BCS) se izostavljaju da uspješna dijeta ne izgleda kao gubitak.
        window_records = [record for record in self.history
                          if record.bcs is None or record.bcs < self.config.bcs_high_min]
        window_weights = [record.weight_kg for record in window_records]
        if len(window_weights) >= self.config.window_min_rows:
            baseline_count = max(2, len(window_weights) - 1) // 2 + 1
            baseline_weights = window_weights[:baseline_count]
            baseline_weight = sum(baseline_weights) / len(baseline_weights)
            recent_weights = [window_weights[-1], self.measurement.weight]
            recent_weight = sum(recent_weights) / len(recent_weights)
            # Šum razlike dva prosjeka težina.
            noise_kg = self.weight_sigma_kg * math.sqrt(1.0 / len(baseline_weights) + 1.0 / len(recent_weights))
            window_loss_kg = baseline_weight - recent_weight
            window_loss_limit_pct = (self.config.window_loss_pct_on_diet if is_overweight
                                     else self.config.window_loss_pct)
            window_loss_limit_kg = max(window_loss_limit_pct / 100.0 * baseline_weight,
                                       self.config.window_alarm_sigma * noise_kg)
            self.diagnostics.update(window_loss_kg=window_loss_kg, window_limit_kg=window_loss_limit_kg)
            if window_loss_kg > window_loss_limit_kg:
                # context["slope"] je u kg/sedmici (kao i u trend grani), pa se gubitak prozora svodi na sedmicu.
                baseline_time_s = sum(record.measured_at.timestamp()
                                      for record in window_records[:baseline_count]) / baseline_count
                recent_time_s = (window_records[-1].measured_at.timestamp()
                                 + self.measurement.measured_at.timestamp()) / 2.0
                window_span_weeks = max((recent_time_s - baseline_time_s) / self.SECONDS_PER_WEEK, 1.0)
                self.context["slope"] = -window_loss_kg / window_span_weeks
                return self.decisions.stop(Action.ESCALATE, weight_loss_code)

        trend = self._trend()
        is_stable = True
        self.context["stable"] = is_stable

        if trend is not None:
            self.diagnostics.update(slope_kg_per_week=trend.slope_kg_per_week, slope_se=trend.se_kg_per_week)
            self.context["slope"] = trend.slope_kg_per_week
            is_stable = abs(trend.slope_kg_per_week) <= max(
                self.config.stable_slope_pct_per_week / 100.0 * self.measurement.weight,
                self.config.stable_sigma * trend.se_kg_per_week)
            self.context.update(stable=is_stable)

            trend_loss_limit_pct = (self.config.rapid_loss_pct_per_week if is_overweight
                                    else self.config.unexplained_loss_pct_per_week)
            loss_alarm_threshold = (trend_loss_limit_pct / 100.0 * self.measurement.weight
                                    + self.config.trend_alarm_sigma * trend.se_kg_per_week)
            if trend.slope_kg_per_week < -loss_alarm_threshold:
                return self.decisions.stop(Action.ESCALATE, weight_loss_code)

        return None

    def _was_not_ideal(self) -> bool:
        """Pas je "upravo stigao na cilj" ako je prošli put imao neidealan BCS, a sada ima idealan."""
        if self.previous_record is None:
            return False
        return (self.calculations.bcs_direction(self.previous_record.bcs) != 0
                and self.calculations.bcs_direction(self.measurement.bcs) == 0)

    def _trend(self) -> Optional[Trend]:
        recent_records = self.history[-(self.config.trend_points - 1):]
        # Tačke (vrijeme u sedmicama u odnosu na trenutno mjerenje, težina u kg); prošlost je negativna.
        weight_points = [((record.measured_at - self.measurement.measured_at).total_seconds() / self.SECONDS_PER_WEEK,
                          record.weight_kg) for record in recent_records]
        weight_points.append((0.0, self.measurement.weight))
        return fit(weight_points, self.weight_sigma_kg)

    def _is_ideal(self) -> CalorieAgentResponse | None:
        classification = self.context["classification"]
        is_stable = self.context["stable"]
        if classification.state is BodyState.IDEAL:
            has_arrived_at_target = self._was_not_ideal()
            self.diagnostics["arrived_at_target"] = has_arrived_at_target
            if is_stable and not has_arrived_at_target:
                self.learned = replace(self.learned, weeks_at_bound=0)
                return self.decisions.stop(Action.NO_CHANGE, "IN_DEAD_ZONE")
            if has_arrived_at_target:
                self.reason_codes.append("TARGET_REACHED")
        return None

    def _regulate(self) -> CalorieAgentResponse:
        return Regulator(self).regulate()
