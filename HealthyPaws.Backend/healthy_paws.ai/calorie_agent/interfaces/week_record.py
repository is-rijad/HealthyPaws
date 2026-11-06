from dataclasses import dataclass
from datetime import datetime
from typing import Optional


@dataclass(frozen=True)
class WeekRecord:
    measured_at: datetime
    weight_kg: float
    bcs: int
    eaten_g_per_day: Optional[float] = None
    offered_g_per_day: Optional[float] = None