import calorie_agent.calorie_agent as ca
from calorie_agent.core.classification.body_state import BodyState


class Controller:
    """Pretvara cilj (ciljna težina) u željenu promjenu težine i prijedlog dnevnih kalorija."""

    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def desired_rate_kg_per_week(self, weight_kg: float) -> float:
        classification = self.agent.context["classification"]
        config = self.agent.config
        weight_gap_kg = classification.target_weight_kg - weight_kg

        # Cilj se postiže kroz target_horizon_weeks, ali promjena nije brža od zadanog % težine sedmično.
        if classification.state is BodyState.OVERWEIGHT:
            max_loss_kg_per_week = config.max_loss_pct_per_week / 100.0 * weight_kg
            return min(0.0, max(weight_gap_kg / config.target_horizon_weeks, -max_loss_kg_per_week))
        if classification.state is BodyState.UNDERWEIGHT:
            max_gain_kg_per_week = config.max_gain_pct_per_week / 100.0 * weight_kg
            return max(0.0, min(weight_gap_kg / config.target_horizon_weeks, max_gain_kg_per_week))
        return 0.0

    def propose_kcal(self, mer_estimate: float, weight_kg: float) -> tuple[float, float]:
        """Vraća (predloženi dnevni kcal, željena promjena težine u kg/sedmici)."""
        rate_kg_per_week = self.desired_rate_kg_per_week(weight_kg)
        daily_energy_surplus_kcal = self.agent.config.kcal_per_kg_body_change * rate_kg_per_week / 7.0
        return mer_estimate + daily_energy_surplus_kcal, rate_kg_per_week
