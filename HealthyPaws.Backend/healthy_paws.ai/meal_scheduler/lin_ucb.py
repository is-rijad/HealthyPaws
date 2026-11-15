from typing import Sequence

import numpy as np

from meal_scheduler.data.arm_state import get_or_create_arm, ArmState
from meal_scheduler.data.lin_ucb_config import LinUCBConfig
from meal_scheduler.data.lin_ucb_state import LinUCBState, check_x


class LinUcb:
    _TIE_EPS = 1e-9
    @staticmethod
    def scores(state: LinUCBState, x: Sequence[float], arms: Sequence[int],
               cfg: LinUCBConfig = LinUCBConfig()) -> dict[int, float]:
        xv = check_x(state, x)
        out: dict[int, float] = {}
        for a in arms:
            s = get_or_create_arm(state, a, cfg)
            ainv_x = np.linalg.solve(s.A, xv)  # A^-1 x, bez eksplicitne inverzije
            theta_x = float(s.b @ ainv_x)  # (A^-1 b)·x = b·(A^-1 x), jer je A simetrična
            bonus = cfg.alpha * float(np.sqrt(max(xv @ ainv_x, 0.0)))
            out[a] = theta_x + bonus
        return out

    @staticmethod
    def select(state: LinUCBState, x: Sequence[float], arms: Sequence[int],
               cfg: LinUCBConfig = LinUCBConfig()) -> tuple[int, dict[int, float]]:
        if not arms:
            raise ValueError("lista armova je prazna")
        if len(set(arms)) != len(arms):
            raise ValueError("armovi nisu jedinstveni")
        sc = LinUcb.scores(state, x, arms, cfg)
        best = max(sc.values())
        tied = [a for a, v in sc.items() if best - v <= LinUcb._TIE_EPS]
        return min(tied, key=lambda a: (abs(a), a)), sc

    @staticmethod
    def update(state: LinUCBState, arm: int, x: Sequence[float], reward: float,
               cfg: LinUCBConfig = LinUCBConfig()) -> LinUCBState:
        if not np.isfinite(reward):
            raise ValueError("reward nije konačan broj")
        xv = check_x(state, x)
        s = get_or_create_arm(state, arm, cfg)
        g = cfg.gamma
        eye = np.eye(state.d)
        A = g * s.A + (1.0 - g) * cfg.lam * eye + np.outer(xv, xv)  # g=1 -> A + x x'
        b = g * s.b + reward * xv
        arms = dict(state.arms or {})
        arms[arm] = ArmState(A=A, b=b, n=s.n + 1)
        return LinUCBState(d=state.d, version=state.version + 1, arms=arms)