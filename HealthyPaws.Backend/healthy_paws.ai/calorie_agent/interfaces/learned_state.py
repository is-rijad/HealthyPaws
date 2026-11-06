import math
from dataclasses import dataclass, replace
from datetime import datetime
from typing import Optional

import calorie_agent.calorie_agent as ca
from rpc.service.calorie_agent import calorie_agent_pb2


@dataclass
class LearnedState:
    mer_estimate_kcal: Optional[float] = None
    variance_kcal2: Optional[float] = None
    n_updates: int = 0
    consecutive_bad_weeks: int = 0
    weeks_at_bound: int = 0
    pending_weight_kg: Optional[float] = None
    last_update_at: Optional[datetime] = None

    @staticmethod
    def sane_learned(agent: ca.CalorieAgent) -> LearnedState:
        learned : LearnedState | None = agent.plan.learned if agent.plan is not None else None
        if learned is None:
            return LearnedState()
        est, var, pending = learned.mer_estimate_kcal, learned.variance_kcal2, learned.pending_weight_kg
        changed = False
        if not (est is None and var is None):
            if not (est is not None and var is not None and math.isfinite(est) and math.isfinite(var)
                    and est > 0 and var > 0):
                est = var = None
                changed = True
        if pending is not None and not (math.isfinite(pending) and pending > 0):
            pending, changed = None, True

        counters = [max(0, c) for c in (learned.n_updates, learned.consecutive_bad_weeks, learned.weeks_at_bound)]
        if counters != [learned.n_updates, learned.consecutive_bad_weeks, learned.weeks_at_bound]:
            changed = True

        if not changed:
            return learned
        agent.reasons.append("LEARNED_STATE_RESET")
        return replace(learned, mer_estimate_kcal=est, variance_kcal2=var, pending_weight_kg=pending,
                       n_updates=counters[0], consecutive_bad_weeks=counters[1], weeks_at_bound=counters[2])

    def to_pb2(self) -> calorie_agent_pb2.LearnedState:
        return calorie_agent_pb2.LearnedState(
            mer_estimate_kcal=self.mer_estimate_kcal,
            variance_kcal2=self.variance_kcal2,
            n_updates=self.n_updates,
            consecutive_bad_weeks=self.consecutive_bad_weeks,
            weeks_at_bound=self.weeks_at_bound,
            pending_weight_kg=self.pending_weight_kg,
            last_update_at=self.last_update_at,
        )