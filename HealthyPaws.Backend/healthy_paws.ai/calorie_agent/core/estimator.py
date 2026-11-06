import math
from dataclasses import dataclass, replace
from datetime import datetime

import calorie_agent.calorie_agent as ca
from calorie_agent.interfaces.learned_state import LearnedState


@dataclass(frozen=True)
class Innovation:
    observed_mer: float
    predicted_mer: float
    innovation: float
    var_pred: float
    var_obs: float
    z: float
    predicted_dw_kg: float
    actual_dw_kg: float


class Estimator:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def init_learned(self, prior_mer: float, base: LearnedState) -> LearnedState:
        return replace(base, mer_estimate_kcal=prior_mer, variance_kcal2=(self.agent.cfg.prior_rel_sd * prior_mer) ** 2)

    def innovation(self, learned: LearnedState, eaten_kcal_per_day: float, w_old: float, w_new: float,
                   dt_days: float, sigma_w: float) -> Innovation:
        assert learned.mer_estimate_kcal is not None and learned.variance_kcal2 is not None

        cfg = self.agent.cfg
        rho = cfg.kcal_per_kg_body_change
        p_pred = learned.variance_kcal2 + cfg.process_sd_kcal_per_week ** 2 * (dt_days / 7.0)
        var_obs = (rho * math.sqrt(2.0) * sigma_w / dt_days) ** 2
        observed = eaten_kcal_per_day - rho * (w_new - w_old) / dt_days
        y = observed - learned.mer_estimate_kcal
        s = p_pred + var_obs

        return Innovation(
            observed_mer=observed,
            predicted_mer=learned.mer_estimate_kcal,
            innovation=y,
            var_pred=p_pred,
            var_obs=var_obs,
            z=y / math.sqrt(s),
            predicted_dw_kg=(eaten_kcal_per_day - learned.mer_estimate_kcal) * dt_days / rho,
            actual_dw_kg=w_new - w_old,
        )

    def apply_update(self, learned: LearnedState, inn: Innovation, measured_at: datetime) -> LearnedState:
        s = inn.var_pred + inn.var_obs
        gain = inn.var_pred / s
        return replace(
            learned,
            mer_estimate_kcal=learned.mer_estimate_kcal + gain * inn.innovation,
            variance_kcal2=(1.0 - gain) * inn.var_pred,
            n_updates=learned.n_updates + 1,
            consecutive_bad_weeks=0,
            last_update_at=measured_at,
        )