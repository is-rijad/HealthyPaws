from dataclasses import dataclass
from datetime import datetime


@dataclass(frozen=True)
class Measurement:
    dog_id: int
    weight: float
    bcs: int
    min_weight: float
    max_weight: float
    measured_at: datetime