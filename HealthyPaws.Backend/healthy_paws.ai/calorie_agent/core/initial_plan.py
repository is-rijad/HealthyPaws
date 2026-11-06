from calorie_agent.core.controller import Controller
from calorie_agent.core.guardrails import GuardRails
import calorie_agent.calorie_agent as ca


class InitialPlan:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def create_initial_plan(self) -> float:
        cfg = self.agent.cfg
        prior = self.agent.ctx["prior_mer"]
        assert self.agent.learned.mer_estimate_kcal is not None

        cls = self.agent.ctx["cls"]
        floor = self.agent.ctx["floor"]
        ceiling = self.agent.ctx["ceiling"]

        controller = Controller(self.agent)

        raw, rate = controller.propose_kcal(float(self.agent.learned.mer_estimate_kcal), self.agent.m.weight)

        step = cfg.early_step_pct * (cfg.conflict_step_scale if cls.conflict else 1.0)

        guardrails = GuardRails(self.agent)
        res = guardrails.apply(raw, prior, floor, ceiling, step,
                               enforce_min_change=False, step_reasons=("WARMUP_STEP_LIMIT",))

        self.agent.ctx.update(step_pct=step, rate=rate)
        self.agent.diag.update(raw_kcal=raw, desired_rate_kg_per_week=rate)
        self.agent.reasons.append("INITIAL_PLAN")
        self.agent.reasons.extend(res.reasons)

        return res.kcal
