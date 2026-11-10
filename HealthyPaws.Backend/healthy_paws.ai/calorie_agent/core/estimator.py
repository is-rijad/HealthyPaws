import math
from dataclasses import dataclass, replace
from datetime import datetime

import calorie_agent.calorie_agent as ca
from calorie_agent.interfaces.learned_state import LearnedState


@dataclass(frozen=True)
class Innovation:
    """Razlika između stvarno opažene i očekivane potrošnje energije (korak Kalmanovog filtera)."""
    observed_mer: float
    predicted_mer: float
    innovation_kcal: float
    predicted_variance: float
    observation_variance: float
    z_score: float
    predicted_weight_change_kg: float
    actual_weight_change_kg: float


class Estimator:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def init_learned(self, prior_mer: float, base: LearnedState) -> LearnedState:
        return replace(base, mer_estimate_kcal=prior_mer,
                       variance_kcal2=(self.agent.config.prior_rel_sd * prior_mer) ** 2)

    def innovation(self, learned: LearnedState, eaten_kcal_per_day: float, old_weight_kg: float,
                   new_weight_kg: float, interval_days: float, weight_sigma_kg: float) -> Innovation:
        assert learned.mer_estimate_kcal is not None and learned.variance_kcal2 is not None

        config = self.agent.config
        kcal_per_kg = config.kcal_per_kg_body_change

        # Nesigurnost procjene raste s vremenom jer potrošnja smije "lutati".
        predicted_variance = (learned.variance_kcal2
                              + config.process_sd_kcal_per_week ** 2 * (interval_days / 7.0))
        # Razlika dvije težine ima šum sqrt(2) * sigma, a pretvara se u kcal/dan dijeljenjem s razmakom.
        observation_variance = (kcal_per_kg * math.sqrt(2.0) * weight_sigma_kg / interval_days) ** 2

        # Energetski bilans: što pas pojede, a ne ode u tkivo, to je potrošio.
        observed_mer = eaten_kcal_per_day - kcal_per_kg * (new_weight_kg - old_weight_kg) / interval_days
        innovation_kcal = observed_mer - learned.mer_estimate_kcal
        total_variance = predicted_variance + observation_variance

        return Innovation(
            observed_mer=observed_mer,
            predicted_mer=learned.mer_estimate_kcal,
            innovation_kcal=innovation_kcal,
            predicted_variance=predicted_variance,
            observation_variance=observation_variance,
            z_score=innovation_kcal / math.sqrt(total_variance),
            predicted_weight_change_kg=(eaten_kcal_per_day - learned.mer_estimate_kcal) * interval_days / kcal_per_kg,
            actual_weight_change_kg=new_weight_kg - old_weight_kg,
        )

    def apply_update(self, learned: LearnedState, innovation: Innovation, measured_at: datetime) -> LearnedState:
        total_variance = innovation.predicted_variance + innovation.observation_variance
        kalman_gain = innovation.predicted_variance / total_variance
        return replace(
            learned,
            mer_estimate_kcal=learned.mer_estimate_kcal + kalman_gain * innovation.innovation_kcal,
            variance_kcal2=(1.0 - kalman_gain) * innovation.predicted_variance,
            n_updates=learned.n_updates + 1,
            consecutive_bad_weeks=0,
            last_update_at=measured_at,
        )
