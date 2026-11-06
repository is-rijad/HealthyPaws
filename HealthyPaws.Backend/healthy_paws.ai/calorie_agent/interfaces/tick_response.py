from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional

from calorie_agent.interfaces.action import Action
from calorie_agent.interfaces.learned_state import LearnedState


@dataclass(frozen=True)
class TickResponse:
    dog_id: int
    measured_at: datetime
    correlation_id: str
    plan_version: int
    action: Action
    plan_changed: bool
    new_daily_kcal: Optional[float]
    new_daily_grams: Optional[float]
    reason_codes: tuple[str, ...]
    explanation: str
    requires_human_review: bool
    next_evaluation_at: datetime
    learned: LearnedState
    diagnostics: dict = field(default_factory=dict)