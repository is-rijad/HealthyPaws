from dataclasses import dataclass


@dataclass(frozen=True)
class LinUCBConfig:
    alpha: float = 0.5
    lam: float = 1.0
    gamma: float = 1.0
