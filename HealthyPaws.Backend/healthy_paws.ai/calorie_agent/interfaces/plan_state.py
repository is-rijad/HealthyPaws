from dataclasses import dataclass, field
from typing import Optional

from calorie_agent.interfaces.learned_state import LearnedState


@dataclass(frozen=True)
class PlanState:
    current_daily_kcal: float
    plan_version: int = 0
    learned: LearnedState = field(default_factory=LearnedState)
