import math
import statistics
from datetime import datetime
from typing import Sequence

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

    def empirical_weight_sigma(self, points: Sequence[tuple[datetime, float]], floor_sigma_kg: float) -> float:
        """Šum vage procijenjen iz same historije; nikad ne ispod `floor_sigma_kg` (konfiguriran/kalibriran šum).

        Za svaku srednju tačku gleda odstupanje od linearne interpolacije susjeda: pravi trend se time poništava,
        a ostaje šum. Za tačke u t0 < t1 < t2 vrijedi Var(r) = sigma^2 * (1 + a^2 + b^2), gdje su a i b težine
        susjeda, pa radi i kad je neka sedmica preskočena. Medijan |r| je otporan na pojedinačni loš unos.
        Premalo tačaka => vraća floor. Zaštita od pogrešno prevelike procjene: gornja granica je
        `noise_max_scale` x floor.
        """
        config = self.config
        ordered = sorted(points, key=lambda point: point[0])
        if len(ordered) < config.noise_min_points:
            return floor_sigma_kg
        scaled_residuals: list[float] = []
        for (t0, w0), (t1, w1), (t2, w2) in zip(ordered, ordered[1:], ordered[2:]):
            span = (t2 - t0).total_seconds()
            if span <= 0:
                continue
            right_weight = (t1 - t0).total_seconds() / span
            left_weight = 1.0 - right_weight
            residual = w1 - (left_weight * w0 + right_weight * w2)
            scaled_residuals.append(abs(residual) / math.sqrt(1.0 + left_weight ** 2 + right_weight ** 2))
        if not scaled_residuals:
            return floor_sigma_kg
        # Medijan |N(0, sigma)| = 0.6745 sigma
        sigma = statistics.median(scaled_residuals) / 0.6745
        return min(max(sigma, floor_sigma_kg), config.noise_max_scale * floor_sigma_kg)
