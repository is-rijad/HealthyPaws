from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Optional, Sequence


@dataclass(frozen=True)
class Trend:
    slope_kg_per_week: float
    se_kg_per_week: float
    n: int


def fit(points: Sequence[tuple[float, float]], sigma_kg: float) -> Optional[Trend]:
    n = len(points)
    if n < 3:
        return None
    t_mean = sum(t for t, _ in points) / n
    w_mean = sum(w for _, w in points) / n
    sxx = sum((t - t_mean) ** 2 for t, _ in points)
    if sxx <= 0:
        return None
    sxy = sum((t - t_mean) * (w - w_mean) for t, w in points)
    return Trend(sxy / sxx, sigma_kg / math.sqrt(sxx), n)