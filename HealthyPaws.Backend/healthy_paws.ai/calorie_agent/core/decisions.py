import math
import statistics
from dataclasses import replace
from datetime import timedelta
from typing import Optional

import calorie_agent.calorie_agent as ca
from calorie_agent.core.explanations import Explanations
from calorie_agent.interfaces.action import PLAN_CHANGING, Action
from rpc.service.calorie_agent.calorie_agent_pb2 import CalorieAgentResponse


class Decisions:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def stop(self, action: Action, *codes: str) -> CalorieAgentResponse:
        self.agent.reasons.extend(codes)
        return self.finish(action)

    def finish(self, action: Action, *, kcal: Optional[float] = None) -> CalorieAgentResponse:
        kcal_out = self.agent.plan.current_daily_kcal if kcal is None and self.agent.plan is not None else kcal

        if kcal_out is not None and not math.isfinite(kcal_out):
            kcal_out = None

        grams = (kcal_out / self.agent.profile.food_kcal_per_gram
                 if kcal_out is not None and math.isfinite(self.agent.profile.food_kcal_per_gram)
                    and self.agent.profile.food_kcal_per_gram > 0 else None)

        if self.agent.plan is not None:
            self.agent.ctx.setdefault("old_kcal", self.agent.plan.current_daily_kcal)
        if kcal_out is not None:
            self.agent.ctx["new_kcal"] = kcal_out
        return CalorieAgentResponse(
            dog_id=self.agent.m.dog_id,
            measured_at=self.agent.m.measured_at,
            correlation_id=self.agent.request.correlation_id,
            plan_version=self.agent.plan.plan_version if self.agent.plan else None,
            action=action._name_,
            plan_changed=action in PLAN_CHANGING,
            new_daily_kcal=kcal_out,
            new_daily_grams=grams,
            reason_codes=tuple(dict.fromkeys(self.agent.reasons)),
            explanation=Explanations.explain(list(dict.fromkeys(self.agent.reasons)), self.agent.ctx),
            requires_human_review=action is Action.ESCALATE,
            next_evaluation_at=self.agent.m.measured_at + timedelta(days=self.agent.cfg.next_evaluation_days),
            learned=self.agent.learned.to_pb2(),
            diagnostics=self.agent.diag,
        )

    def confirm_or_remeasure(self) -> CalorieAgentResponse:
        m, cfg = self.agent.m, self.agent.cfg
        pending = self.agent.learned.pending_weight_kg if self.agent.learned is not None else None
        tolerance = max(cfg.remeasure_confirm_tolerance_pct, 3.0 * self.agent.sigma_w / m.weight * 100.0)
        if pending is not None and abs(m.weight - pending) / pending * 100.0 <= tolerance:
            self.agent.learned = replace(self.agent.learned, pending_weight_kg=None)
            return self.stop(Action.ESCALATE, "WEIGHT_OUT_OF_RANGE_CONFIRMED")
        self.agent.learned = replace(self.agent.learned, pending_weight_kg=m.weight)
        return self.stop(Action.REQUEST_REMEASURE, "INVALID_MEASUREMENT")

    def plausibility(self) -> Optional[CalorieAgentResponse]:
        prev, m, cfg = self.agent.prev, self.agent.m, self.agent.cfg
        if prev is None or self.agent.dt_days is None:
            return None

        recent = [r.weight_kg for r in self.agent.hist[-3:]]
        k = len(recent)
        ref = statistics.median(recent)
        weeks = self.agent.dt_days / 7.0 + (1.0 if k > 1 else 0.0)
        change_kg = abs(m.weight - ref)

        allowed_kg = max(cfg.max_plausible_change_pct_per_week / 100.0 * ref * weeks,
                         3.0 * math.sqrt(1.0 + 1.0 / k) * self.agent.sigma_w)
        self.agent.diag["change_pct_per_week"] = change_kg / ref * 100.0 / weeks
        if change_kg <= allowed_kg:
            if self.agent.learned is not None and self.agent.learned.pending_weight_kg is not None:
                self.agent.learned = replace(self.agent.learned, pending_weight_kg=None)
            return None

        self.agent.ctx["prev_weight"] = ref
        pending = self.agent.learned.pending_weight_kg if self.agent.learned is not None else None
        tolerance = max(cfg.remeasure_confirm_tolerance_pct, 3.0 * self.agent.sigma_w / m.weight * 100.0)

        if pending is not None and abs(m.weight - pending) / pending * 100.0 <= tolerance:
            self.agent.learned = replace(self.agent.learned, pending_weight_kg=None)
            return self.stop(Action.ESCALATE, "RAPID_CHANGE_CONFIRMED")

        self.agent.learned = replace(self.agent.learned, pending_weight_kg=m.weight)
        return self.stop(Action.REQUEST_REMEASURE, "REMEASURE_IMPLAUSIBLE_CHANGE")