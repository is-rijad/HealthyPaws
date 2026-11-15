from dataclasses import dataclass
from typing import Mapping, Sequence

import numpy as np

import meal_scheduler.data.arm_state as arm_state


@dataclass(frozen=True)
class LinUCBState:
    d: int
    version: int = 0
    arms: Mapping[int, arm_state.ArmState] | None = None

    @staticmethod
    def new(d: int) -> LinUCBState:
        return LinUCBState(d=d, version=0, arms={})

def check_x(state: LinUCBState, x: Sequence[float]) -> np.ndarray:
    v = np.asarray(x, dtype=float)
    if v.shape != (state.d,):
        raise ValueError(f"kontekst mora imati dimenziju {state.d}, dobijeno {v.shape}")
    if not np.all(np.isfinite(v)):
        raise ValueError("kontekst sadrži NaN/inf")
    return v