"""Virtualni pas: SKRIVENA istina koju agent ne vidi (odluka 7).

Namjerno se razlikuje od agentovog modela, da test ne bi bio "ispit koji pišeš
sam sebi":
  * energetska gustoća tkiva je drugačija nego u agentovom config-u (7700 vs 7000),
  * potrošnja prati težinu (MER ~ W^0.75), pa se pas prilagođava dijeti,
  * može imati skrivene poslastice, skriveni "odliv" energije (bolest) i slab apetit.
"""
from __future__ import annotations

from dataclasses import dataclass


@dataclass
class VirtualDog:
    weight_kg: float
    mer_at_start_kcal: float          # skrivena stvarna potrošnja pri početnoj težini
    start_weight_kg: float
    kcal_per_kg: float = 7700.0       # skrivena fizika (agent pretpostavlja 7000)
    appetite: float = 1.0             # dio ponuđene hrane koji pas pojede
    hidden_treats_kcal: float = 0.0   # kalorije koje hranilica NE vidi
    drain_kcal: float = 0.0           # bolest: gubi energiju bez obzira na unos

    def true_mer(self) -> float:
        return self.mer_at_start_kcal * (self.weight_kg / self.start_weight_kg) ** 0.75

    def step_day(self, offered_kcal: float) -> float:
        """Jedan dan. Vraća kalorije koje je hranilica VIDJELA da je pas pojeo."""
        eaten_feeder = offered_kcal * self.appetite
        intake = eaten_feeder + self.hidden_treats_kcal
        self.weight_kg += (intake - self.true_mer() - self.drain_kcal) / self.kcal_per_kg
        return eaten_feeder

    def true_bcs(self, ideal_kg: float, pct_per_step: float = 20.0) -> int:
        """Agent prihvata samo BCS 1/3/5/7/9, pa je svaki korak skale ~20% odstupanja od idealne težine."""
        steps = (self.weight_kg / ideal_kg - 1.0) * 100.0 / pct_per_step
        steps = int(steps + 0.5) if steps >= 0 else int(steps - 0.5)
        return min(9, max(1, 5 + 2 * steps))
