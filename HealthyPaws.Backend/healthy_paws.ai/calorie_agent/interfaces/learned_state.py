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
        """Čisti naučeno stanje iz baze: nevaljane vrijednosti se odbacuju, a brojači ne smiju biti negativni."""
        stored: LearnedState | None = agent.plan.learned if agent.plan is not None else None
        if stored is None:
            return LearnedState()
        mer_estimate, variance, pending_weight = stored.mer_estimate_kcal, stored.variance_kcal2, stored.pending_weight_kg
        was_modified = False
        # Procjena i varijansa vrijede samo zajedno: obje nepostavljene, ili obje konačne i pozitivne.
        if not (mer_estimate is None and variance is None):
            if not (mer_estimate is not None and variance is not None
                    and math.isfinite(mer_estimate) and math.isfinite(variance)
                    and mer_estimate > 0 and variance > 0):
                mer_estimate = variance = None
                was_modified = True
        if pending_weight is not None and not (math.isfinite(pending_weight) and pending_weight > 0):
            pending_weight, was_modified = None, True

        stored_counters = [stored.n_updates, stored.consecutive_bad_weeks, stored.weeks_at_bound]
        counters = [max(0, counter) for counter in stored_counters]
        if counters != stored_counters:
            was_modified = True

        if not was_modified:
            return stored
        agent.reason_codes.append("LEARNED_STATE_RESET")
        return replace(stored, mer_estimate_kcal=mer_estimate, variance_kcal2=variance,
                       pending_weight_kg=pending_weight, n_updates=counters[0],
                       consecutive_bad_weeks=counters[1], weeks_at_bound=counters[2])

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