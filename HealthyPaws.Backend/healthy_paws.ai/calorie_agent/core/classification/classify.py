import statistics
from typing import Optional

import calorie_agent.calorie_agent as ca
from calorie_agent.core.classification.body_state import BodyState
from calorie_agent.core.classification.classification import Classification
from calorie_agent.interfaces.action import Action


class Classify:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def classify_and_check(self):
        cls = self._classify()
        self.agent.ctx["cls"] = cls
        self.agent.diag.update(state=cls.state.value, conflict=cls.conflict, target_weight_kg=cls.target_weight_kg)

        if cls.bcs_unconfirmed:
            self.agent.reasons.append("BCS_UNCONFIRMED")
        if cls.contradiction:
            return self.agent.decisions.stop(Action.ESCALATE, "CONTRADICTORY_BCS_WEIGHT")

        w_ref = statistics.median([r.weight_kg for r in self.agent.hist[-3:]] + [self.agent.m.weight])
        floor, ceiling = self.agent.calculations.kcal_bounds(w_ref, (self.agent.m.min_weight + self.agent.m.max_weight) / 2.0)
        self.agent.diag["bounds_weight_kg"] = w_ref
        self.agent.ctx.update(floor=floor, ceiling=ceiling)
        self.agent.diag.update(floor_kcal=floor, ceiling_kcal=ceiling)

        return None

    def _classify(self, prev_bcs: Optional[int] = None) -> Classification:
        m = self.agent.m

        bcs_dir = self.agent.calculations.bcs_direction(m.bcs)
        weight_dir = -1 if m.weight < m.min_weight else (1 if m.weight > m.max_weight else 0)
        mid = (m.min_weight + m.max_weight) / 2.0

        if bcs_dir * weight_dir == -1:
            return Classification(BodyState.IDEAL, False, True, m.weight, bcs_dir, weight_dir)

        unconfirmed = False
        if bcs_dir == weight_dir:
            eff = bcs_dir
        elif prev_bcs is not None and self.agent.calculations.bcs_direction(prev_bcs) == bcs_dir:
            eff = bcs_dir
        else:
            eff = weight_dir
            unconfirmed = True

        if eff == 1:
            target = mid if weight_dir == 1 else m.min_weight
            return Classification(BodyState.OVERWEIGHT, weight_dir != 1, False, target, bcs_dir, weight_dir,
                                  unconfirmed)
        if eff == -1:
            target = mid if weight_dir == -1 else m.max_weight
            return Classification(BodyState.UNDERWEIGHT, weight_dir != -1, False, target, bcs_dir, weight_dir,
                                  unconfirmed)

        return Classification(BodyState.IDEAL, weight_dir != 0, False, m.weight, bcs_dir, weight_dir, unconfirmed)