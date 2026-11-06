from dataclasses import replace

import calorie_agent.calorie_agent as ca
from calorie_agent.core.controller import Controller
from calorie_agent.core.guardrails import GuardRails
from calorie_agent.interfaces.action import Action


class Regulator:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent


    def regulate(self):
        cfg, m = self.agent.cfg, self.agent.m
        current = self.agent.plan.current_daily_kcal
        cls = self.agent.ctx["cls"]
        floor = self.agent.ctx["floor"]
        ceiling = self.agent.ctx["ceiling"]
        assert current is not None

        controller = Controller(self.agent)
        raw, rate = controller.propose_kcal(self.agent.ctx["mer_est"], m.weight)
        raw = current + cfg.plan_smoothing * (raw - current)
        guardrails = GuardRails(self.agent)
        step = guardrails.step_limit_pct(self.agent.learned.n_updates, cls.conflict)

        if cls.conflict:
            step_reasons = ("CONFLICT_STEP_LIMIT",)
        elif self.agent.learned.n_updates < cfg.warmup_updates:
            step_reasons = ("WARMUP_STEP_LIMIT",)
        else:
            step_reasons = ("RATE_LIMIT_APPLIED",)

        res = guardrails.apply(raw, current, floor, ceiling, step, step_reasons=step_reasons)
        self.agent.ctx.update(step_pct=step, rate=rate)
        self.agent.diag.update(raw_kcal=raw, desired_rate_kg_per_week=rate, step_limit_pct=step)

        stuck = (res.at_floor and current <= floor * 1.01) or (res.at_ceiling and current >= ceiling * 0.99)
        weeks = self.agent.learned.weeks_at_bound + 1 if stuck else 0
        self.agent.learned = replace(self.agent.learned, weeks_at_bound=weeks)

        if weeks >= cfg.bound_weeks_before_escalation:
            self.agent.ctx["new_kcal"] = res.kcal
            return self.agent.decisions.stop(Action.ESCALATE, "PLAN_AT_BOUND_NO_PROGRESS")

        if rate < 0:
            primary = "WEIGHT_LOSS_TARGET"
        elif rate > 0:
            primary = "WEIGHT_GAIN_TARGET"
        else:
            primary = "MAINTENANCE_TARGET"

        self.agent.reasons.append(primary)
        self.agent.reasons.extend(res.reasons)
        if not res.changed:
            return self.agent.decisions.finish(Action.NO_CHANGE)

        return self.agent.decisions.finish(Action.INCREASE if res.kcal > current else Action.DECREASE, kcal=res.kcal)