from dataclasses import dataclass
from typing import Optional


@dataclass(frozen=True)
class IntakeSummary:
    eaten_g_per_day: Optional[float]
    offered_g_per_day: Optional[float]
    days_covered: float
    period_days: float