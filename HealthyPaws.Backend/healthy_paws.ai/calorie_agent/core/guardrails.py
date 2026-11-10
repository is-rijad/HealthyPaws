from __future__ import annotations

from dataclasses import dataclass

import calorie_agent.calorie_agent as ca


@dataclass(frozen=True)
class GuardResult:
    kcal: float
    reason_codes: tuple[str, ...]
    at_floor: bool
    at_ceiling: bool
    changed: bool


class GuardRails:
    """Sigurnosne ograde nad predloženom dozom: koridor [floor, ceiling], najveći korak i najmanja vrijedna promjena."""

    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def step_limit_pct(self, n_updates: int, conflict: bool) -> float:
        config = self.agent.config
        base_step_pct = config.early_step_pct if n_updates < config.warmup_updates else config.max_step_pct
        return base_step_pct * (config.conflict_step_scale if conflict else 1.0)

    @staticmethod
    def _clamp(value: float, lower: float, upper: float) -> float:
        return max(lower, min(upper, value))

    def apply(self, proposed_kcal: float, reference_kcal: float, floor_kcal: float, ceiling_kcal: float,
              step_pct: float, *, enforce_min_change: bool = True,
              step_reason_codes: tuple[str, ...] = ()) -> GuardResult:
        reason_codes: list[str] = []
        at_floor = proposed_kcal < floor_kcal
        at_ceiling = proposed_kcal > ceiling_kcal

        if at_floor:
            reason_codes.append("FLOOR_APPLIED")
        if at_ceiling:
            reason_codes.append("CEILING_APPLIED")

        kcal = GuardRails._clamp(proposed_kcal, floor_kcal, ceiling_kcal)

        # Najveći dozvoljeni korak u odnosu na referentnu dozu.
        min_stepped_kcal = reference_kcal * (1.0 - step_pct / 100.0)
        max_stepped_kcal = reference_kcal * (1.0 + step_pct / 100.0)
        step_limited_kcal = GuardRails._clamp(kcal, min_stepped_kcal, max_stepped_kcal)
        if step_limited_kcal != kcal:
            reason_codes.extend(step_reason_codes or ("RATE_LIMIT_APPLIED",))
        kcal = step_limited_kcal

        # Granice imaju prednost nad ograničenjem koraka.
        bounded_kcal = GuardRails._clamp(kcal, floor_kcal, ceiling_kcal)
        reference_in_bounds = floor_kcal <= reference_kcal <= ceiling_kcal
        if bounded_kcal != kcal:
            reason_codes.append("PLAN_OUTSIDE_SAFE_BOUNDS" if not reference_in_bounds else "BOUNDS_OVERRIDE_STEP")
        kcal = bounded_kcal

        # Promjena manja od praga je ispod šuma mjerenja, pa se plan ne dira.
        change_pct = abs(kcal - reference_kcal) / reference_kcal * 100.0
        if enforce_min_change and reference_in_bounds and change_pct < self.agent.config.min_change_pct:
            reason_codes.append("CHANGE_BELOW_THRESHOLD")
            kcal = reference_kcal

        return GuardResult(kcal, tuple(reason_codes), at_floor, at_ceiling, kcal != reference_kcal)
