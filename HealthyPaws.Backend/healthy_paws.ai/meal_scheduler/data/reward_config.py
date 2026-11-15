import os
from dataclasses import dataclass, field


def _window_s() -> float:
    # isti env var čita i backend (BanditOptions.RewardWindow), da se prozor ne može razići
    try:
        v = float(os.environ.get("BANDIT_REWARD_WINDOW_S", 1800.0))
        return v if v > 0 else 1800.0
    except ValueError:
        return 1800.0


@dataclass(frozen=True)
class RewardConfig:
    w_eaten: float = 0.6     # udio pojedenog u prozoru
    w_latency: float = 0.4   # brzina prvog zalogaja
    window_s: float = field(default_factory=_window_s)  # prozor mjerenja nakon ispuštanja (default 30 min)
