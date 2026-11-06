import math

from calorie_agent.config.config import CalorieAgentConfig
from calorie_agent.interfaces.dog_profile import DogProfile


class Calculations:
    def __init__(self, cfg: CalorieAgentConfig):
        self.cfg = cfg

    def rer(self, weight_kg: float) -> float:
        return self.cfg.rer_coeff * weight_kg ** self.cfg.rer_exponent

    def mer_prior(self, weight_kg: float, profile: DogProfile) -> float:
        base = self.cfg.mer_factor_neutered if profile.neutered else self.cfg.mer_factor_intact
        return self.rer(weight_kg) * base * float(profile.activity_factor)

    def kcal_bounds(self, weight_kg: float, target_weight_kg: float) -> tuple[float, float]:
        cfg = self.cfg
        floor = self.rer(min(weight_kg, target_weight_kg)) * cfg.min_rer_multiple
        ceiling = self.rer(max(weight_kg, target_weight_kg)) * cfg.max_rer_multiple
        return floor, ceiling

    def weight_sigma(self, weight_kg: float) -> float:
        cfg = self.cfg
        return math.hypot(cfg.weight_sigma_abs_kg, cfg.weight_sigma_rel * weight_kg)

    def bcs_direction(self, bcs: int) -> int:
        return -1 if bcs <= self.cfg.bcs_low_max else (1 if bcs >= self.cfg.bcs_high_min else 0)