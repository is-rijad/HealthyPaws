from dataclasses import dataclass


@dataclass(frozen=True)
class DogProfile:
    life_stage: str
    neutered: bool
    activity_factor: str
    pregnant_or_lactating: bool
    food_kcal_per_gram: float