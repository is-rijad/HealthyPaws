from dataclasses import replace

import calorie_agent.calorie_agent as ca
from calorie_agent.core.controller import Controller
from calorie_agent.core.guardrails import GuardRails
from calorie_agent.interfaces.action import Action


class Regulator:
    """Redovna regulacija: pomjera trenutnu dozu prema procijenjenoj potrošnji uz cilj promjene težine."""

    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def regulate(self):
        agent = self.agent
        config, measurement = agent.config, agent.measurement
        current_kcal = agent.plan.current_daily_kcal
        classification = agent.context["classification"]
        floor_kcal = agent.context["floor"]
        ceiling_kcal = agent.context["ceiling"]
        assert current_kcal is not None

        proposed_kcal, rate_kg_per_week = Controller(agent).propose_kcal(
            agent.context["mer_estimate"], measurement.weight)
        # Izglađivanje: ide se samo dio puta prema prijedlogu da doza ne oscilira.
        smoothed_kcal = current_kcal + config.plan_smoothing * (proposed_kcal - current_kcal)

        guardrails = GuardRails(agent)
        step_pct = guardrails.step_limit_pct(agent.learned.n_updates, classification.conflict)

        if classification.conflict:
            step_reason_codes = ("CONFLICT_STEP_LIMIT",)
        elif agent.learned.n_updates < config.warmup_updates:
            step_reason_codes = ("WARMUP_STEP_LIMIT",)
        else:
            step_reason_codes = ("RATE_LIMIT_APPLIED",)

        result = guardrails.apply(smoothed_kcal, current_kcal, floor_kcal, ceiling_kcal, step_pct,
                                  step_reason_codes=step_reason_codes)
        agent.context.update(step_pct=step_pct, rate=rate_kg_per_week)
        agent.diagnostics.update(raw_kcal=smoothed_kcal, desired_rate_kg_per_week=rate_kg_per_week,
                                 step_limit_pct=step_pct)

        # Zaglavljen: doza je već na granici, a prijedlog traži da ode dalje.
        is_stuck_at_bound = ((result.at_floor and current_kcal <= floor_kcal * 1.01)
                             or (result.at_ceiling and current_kcal >= ceiling_kcal * 0.99))
        weeks_at_bound = agent.learned.weeks_at_bound + 1 if is_stuck_at_bound else 0
        agent.learned = replace(agent.learned, weeks_at_bound=weeks_at_bound)

        if weeks_at_bound >= config.bound_weeks_before_escalation:
            agent.context["new_kcal"] = result.kcal
            return agent.decisions.stop(Action.ESCALATE, "PLAN_AT_BOUND_NO_PROGRESS")

        if rate_kg_per_week < 0:
            target_reason_code = "WEIGHT_LOSS_TARGET"
        elif rate_kg_per_week > 0:
            target_reason_code = "WEIGHT_GAIN_TARGET"
        else:
            target_reason_code = "MAINTENANCE_TARGET"

        agent.reason_codes.append(target_reason_code)
        agent.reason_codes.extend(result.reason_codes)
        if not result.changed:
            return agent.decisions.finish(Action.NO_CHANGE)

        action = Action.INCREASE if result.kcal > current_kcal else Action.DECREASE
        return agent.decisions.finish(action, kcal=result.kcal)
