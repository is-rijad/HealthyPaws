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
    """Gradi odgovore agenta i vodi dvostepenu potvrdu sumnjivih težina (pending_weight_kg)."""

    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def stop(self, action: Action, *reason_codes: str) -> CalorieAgentResponse:
        self.agent.reason_codes.extend(reason_codes)
        return self.finish(action, has_error=True)

    def finish(self, action: Action, *, kcal: Optional[float] = None,
               has_error: Optional[bool] = None) -> CalorieAgentResponse:
        agent = self.agent
        daily_kcal = agent.plan.current_daily_kcal if kcal is None and agent.plan is not None else kcal

        if daily_kcal is not None and not math.isfinite(daily_kcal):
            daily_kcal = None

        food_kcal_per_gram = agent.profile.food_kcal_per_gram
        daily_grams = (daily_kcal / food_kcal_per_gram
                       if daily_kcal is not None and math.isfinite(food_kcal_per_gram) and food_kcal_per_gram > 0
                       else None)

        if agent.plan is not None:
            agent.context.setdefault("old_kcal", agent.plan.current_daily_kcal)
        if daily_kcal is not None:
            agent.context["new_kcal"] = daily_kcal

        unique_reason_codes = list(dict.fromkeys(agent.reason_codes))
        return CalorieAgentResponse(
            dog_id=agent.measurement.dog_id,
            measured_at=agent.measurement.measured_at,
            correlation_id=agent.request.correlation_id,
            plan_version=agent.plan.plan_version if agent.plan else None,
            action=action._name_,
            plan_changed=action in PLAN_CHANGING,
            new_daily_kcal=daily_kcal,
            new_daily_grams=daily_grams,
            reason_codes=tuple(unique_reason_codes),
            explanation=Explanations.explain(unique_reason_codes, agent.context),
            requires_human_review=action is Action.ESCALATE,
            next_evaluation_at=agent.measurement.measured_at + timedelta(days=agent.config.next_evaluation_days),
            learned=agent.learned.to_pb2(),
            diagnostics=agent.diagnostics,
            has_error=has_error if has_error is not None else False,
        )

    def confirm_or_remeasure(self) -> CalorieAgentResponse:
        """Težina izvan vjerodostojnog opsega: drugo vaganje je mora potvrditi, inače se traži ponovno vaganje."""
        measurement = self.agent.measurement
        if self._matches_pending_weight():
            self._clear_pending_weight()
            return self.stop(Action.ESCALATE, "WEIGHT_OUT_OF_RANGE_CONFIRMED")
        self._set_pending_weight(measurement.weight)
        return self.stop(Action.REQUEST_REMEASURE, "INVALID_MEASUREMENT")

    def plausibility(self) -> Optional[CalorieAgentResponse]:
        """Nagla promjena težine: traži ponovno vaganje, a tek kad ga drugo vaganje potvrdi, eskalira."""
        agent = self.agent
        measurement, config = agent.measurement, agent.config
        if agent.previous_record is None or agent.days_since_previous is None:
            return None

        recent_weights = [record.weight_kg for record in agent.history[-3:]]
        recent_count = len(recent_weights)
        # Medijan zadnje tri težine je otporan na jedan loš unos.
        reference_weight = statistics.median(recent_weights)
        # Za više tačaka medijan je "u prošlosti", pa se dodaje sedmica tolerancije.
        elapsed_weeks = agent.days_since_previous / 7.0 + (1.0 if recent_count > 1 else 0.0)
        change_kg = abs(measurement.weight - reference_weight)

        allowed_change_kg = max(
            config.max_plausible_change_pct_per_week / 100.0 * reference_weight * elapsed_weeks,
            3.0 * math.sqrt(1.0 + 1.0 / recent_count) * agent.weight_sigma_kg)
        agent.diagnostics["change_pct_per_week"] = change_kg / reference_weight * 100.0 / elapsed_weeks
        if change_kg <= allowed_change_kg:
            if agent.learned is not None and agent.learned.pending_weight_kg is not None:
                self._clear_pending_weight()
            return None

        agent.context["prev_weight"] = reference_weight
        if self._matches_pending_weight():
            self._clear_pending_weight()
            return self.stop(Action.ESCALATE, "RAPID_CHANGE_CONFIRMED")

        self._set_pending_weight(measurement.weight)
        return self.stop(Action.REQUEST_REMEASURE, "REMEASURE_IMPLAUSIBLE_CHANGE")

    def _matches_pending_weight(self) -> bool:
        """Da li se trenutna težina poklapa (unutar tolerancije) s prethodnom sumnjivom težinom."""
        agent = self.agent
        weight = agent.measurement.weight
        pending_weight = agent.learned.pending_weight_kg if agent.learned is not None else None
        if pending_weight is None:
            return False
        tolerance_pct = max(agent.config.remeasure_confirm_tolerance_pct,
                            3.0 * agent.weight_sigma_kg / weight * 100.0)
        return abs(weight - pending_weight) / pending_weight * 100.0 <= tolerance_pct

    def _set_pending_weight(self, weight_kg: float) -> None:
        self.agent.learned = replace(self.agent.learned, pending_weight_kg=weight_kg)

    def _clear_pending_weight(self) -> None:
        self.agent.learned = replace(self.agent.learned, pending_weight_kg=None)
