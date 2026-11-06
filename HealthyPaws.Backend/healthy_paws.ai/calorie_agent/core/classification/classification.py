from dataclasses import dataclass

from calorie_agent.core.classification.body_state import BodyState


@dataclass(frozen=True)
class Classification:
    state: BodyState
    conflict: bool
    contradiction: bool
    target_weight_kg: float
    bcs_dir: int
    weight_dir: int
    bcs_unconfirmed: bool = False