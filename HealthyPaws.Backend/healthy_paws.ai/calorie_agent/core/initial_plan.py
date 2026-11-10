from calorie_agent.core.controller import Controller
from calorie_agent.core.guardrails import GuardRails
import calorie_agent.calorie_agent as ca


class InitialPlan:
    """Prva doza za psa koji još nema plan: polazi od formule (priora), uz blag prvi korak."""

    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def create_initial_plan(self) -> float:
        config = self.agent.config
        prior_mer = self.agent.context["prior_mer"]
        assert self.agent.learned.mer_estimate_kcal is not None

        classification = self.agent.context["classification"]
        floor_kcal = self.agent.context["floor"]
        ceiling_kcal = self.agent.context["ceiling"]

        proposed_kcal, rate_kg_per_week = Controller(self.agent).propose_kcal(
            float(self.agent.learned.mer_estimate_kcal), self.agent.measurement.weight)

        step_pct = config.early_step_pct * (config.conflict_step_scale if classification.conflict else 1.0)

        # Referenca je prior jer prethodnog plana nema; minimalna promjena se ne provjerava jer plan mora nastati.
        result = GuardRails(self.agent).apply(
            proposed_kcal, prior_mer, floor_kcal, ceiling_kcal, step_pct,
            enforce_min_change=False, step_reason_codes=("WARMUP_STEP_LIMIT",))

        self.agent.context.update(step_pct=step_pct, rate=rate_kg_per_week)
        self.agent.diagnostics.update(raw_kcal=proposed_kcal, desired_rate_kg_per_week=rate_kg_per_week)
        self.agent.reason_codes.append("INITIAL_PLAN")
        self.agent.reason_codes.extend(result.reason_codes)

        return result.kcal
