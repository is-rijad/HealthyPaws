from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Optional, Sequence


@dataclass(frozen=True)
class Trend:
    slope_kg_per_week: float
    se_kg_per_week: float
    n: int


def fit(points: Sequence[tuple[float, float]], weight_sigma_kg: float) -> Optional[Trend]:
    """Linearna regresija težine po vremenu; tačke su (vrijeme u sedmicama, težina u kg)."""
    point_count = len(points)
    if point_count < 3:
        return None
    mean_time = sum(time for time, _ in points) / point_count
    mean_weight = sum(weight for _, weight in points) / point_count
    time_variation = sum((time - mean_time) ** 2 for time, _ in points)
    if time_variation <= 0:
        return None
    covariation = sum((time - mean_time) * (weight - mean_weight) for time, weight in points)
    return Trend(covariation / time_variation, weight_sigma_kg / math.sqrt(time_variation), point_count)
