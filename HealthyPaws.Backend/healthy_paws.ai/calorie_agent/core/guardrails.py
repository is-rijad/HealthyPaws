from __future__ import annotations

from dataclasses import dataclass

import calorie_agent.calorie_agent as ca


@dataclass(frozen=True)
class GuardResult:
    kcal: float
    reasons: tuple[str, ...]
    at_floor: bool
    at_ceiling: bool
    changed: bool

class GuardRails:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def step_limit_pct(self, n_updates: int, conflict: bool) -> float:
        cfg = self.agent.cfg
        base = cfg.early_step_pct if n_updates < cfg.warmup_updates else cfg.max_step_pct
        return base * (cfg.conflict_step_scale if conflict else 1.0)

    @staticmethod
    def _clamp(x: float, lo: float, hi: float) -> float:
        return max(lo, min(hi, x))

    def apply(self, raw_kcal: float, reference_kcal: float, floor: float, ceiling: float,
              step_pct: float, *, enforce_min_change: bool = True,
              step_reasons: tuple[str, ...] = ()) -> GuardResult:
        reasons: list[str] = []
        at_floor = raw_kcal < floor
        at_ceiling = raw_kcal > ceiling

        if at_floor:
            reasons.append("FLOOR_APPLIED")
        if at_ceiling:
            reasons.append("CEILING_APPLIED")

        x = GuardRails._clamp(raw_kcal, floor, ceiling)

        lo = reference_kcal * (1.0 - step_pct / 100.0)
        hi = reference_kcal * (1.0 + step_pct / 100.0)
        stepped = GuardRails._clamp(x, lo, hi)
        if stepped != x:
            reasons.extend(step_reasons or ("RATE_LIMIT_APPLIED",))
        x = stepped

        bounded = GuardRails._clamp(x, floor, ceiling)
        reference_in_bounds = floor <= reference_kcal <= ceiling
        if bounded != x:
            reasons.append("PLAN_OUTSIDE_SAFE_BOUNDS" if not reference_in_bounds else "BOUNDS_OVERRIDE_STEP")
        x = bounded

        if enforce_min_change and reference_in_bounds and abs(x - reference_kcal) / reference_kcal * 100.0 < self.agent.cfg.min_change_pct:
            reasons.append("CHANGE_BELOW_THRESHOLD")
            x = reference_kcal

        return GuardResult(x, tuple(reasons), at_floor, at_ceiling, x != reference_kcal)