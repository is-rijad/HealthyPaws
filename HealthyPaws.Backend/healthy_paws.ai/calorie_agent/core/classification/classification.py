from dataclasses import dataclass

from calorie_agent.core.classification.body_state import BodyState


@dataclass(frozen=True)
class Classification:
    """Stanje tijela psa. Odstupanja (deviation) su -1 (ispod idealnog), 0 (idealno) ili +1 (iznad idealnog)."""
    state: BodyState
    conflict: bool
    contradiction: bool
    target_weight_kg: float
    bcs_deviation: int
    weight_deviation: int
    bcs_unconfirmed: bool = False
