import calorie_agent.calorie_agent as ca
from calorie_agent.core.classification.body_state import BodyState


class Controller:
    def __init__(self, agent : ca.CalorieAgent):
        self.agent = agent

    def desired_rate_kg_per_week(self, weight_kg: float) -> float:
        cls = self.agent.ctx["cls"]
        cfg = self.agent.cfg
        gap = cls.target_weight_kg - weight_kg

        if cls.state is BodyState.OVERWEIGHT:
            return min(0.0, max(gap / cfg.target_horizon_weeks, -cfg.max_loss_pct_per_week / 100.0 * weight_kg))
        if cls.state is BodyState.UNDERWEIGHT:
            return max(0.0, min(gap / cfg.target_horizon_weeks, cfg.max_gain_pct_per_week / 100.0 * weight_kg))
        return 0.0

    def propose_kcal(self, mer_estimate: float, weight_kg: float) -> tuple[
        float, float]:
        rate = self.desired_rate_kg_per_week(weight_kg)
        return mer_estimate + self.agent.cfg.kcal_per_kg_body_change * rate / 7.0, rate