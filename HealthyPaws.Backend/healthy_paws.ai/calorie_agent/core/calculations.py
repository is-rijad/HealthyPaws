import math

from calorie_agent.config.config import CalorieAgentConfig
from calorie_agent.interfaces.dog_profile import DogProfile


class Calculations:
    def __init__(self, config: CalorieAgentConfig):
        self.config = config

    def rer(self, weight_kg: float) -> float:
        return self.config.rer_coeff * weight_kg ** self.config.rer_exponent

    def mer_prior(self, weight_kg: float, profile: DogProfile) -> float:
        base = self.config.mer_factor_neutered if profile.neutered else self.config.mer_factor_intact
        return self.rer(weight_kg) * base * float(profile.activity_factor)

    def kcal_bounds(self, weight_kg: float, target_weight_kg: float) -> tuple[float, float]:
        config = self.config
        floor = self.rer(min(weight_kg, target_weight_kg)) * config.min_rer_multiple
        ceiling = self.rer(max(weight_kg, target_weight_kg)) * config.max_rer_multiple
        return floor, ceiling

    def weight_sigma(self, weight_kg: float) -> float:
        config = self.config
        return math.hypot(config.weight_sigma_abs_kg, config.weight_sigma_rel * weight_kg)

    def bcs_direction(self, bcs: int | None) -> int:
        if bcs is None:
            return 0
        return -1 if bcs <= self.config.bcs_low_max else (1 if bcs >= self.config.bcs_high_min else 0)