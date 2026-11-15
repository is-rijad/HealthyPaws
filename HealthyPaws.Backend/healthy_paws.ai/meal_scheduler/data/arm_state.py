from dataclasses import dataclass

import numpy as np

from meal_scheduler.data.lin_ucb_config import LinUCBConfig
import meal_scheduler.data.lin_ucb_state as lin_state


@dataclass(frozen=True)
class ArmState:
    A: np.ndarray
    b: np.ndarray
    n: int = 0

def get_or_create_arm(state: lin_state.LinUCBState, arm: int, cfg: LinUCBConfig) -> ArmState:
    existing = (state.arms or {}).get(arm)
    if existing is not None:
        return existing
    return ArmState(A=cfg.lam * np.eye(state.d), b=np.zeros(state.d), n=0)