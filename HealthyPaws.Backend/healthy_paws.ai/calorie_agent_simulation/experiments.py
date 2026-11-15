"""Eksperimenti iz RESULTS.md: `python -m calorie_agent_simulation.experiments <noise|gain|spread|all> [seedova]`.

  noise   adaptivna procjena šuma vage: bez nje (noise_max_scale=1), fiksni sigma 0.35 i trenutno ponašanje
  gain    max_gain_pct_per_week 1.0 / 1.5 / 2.0 / 3.0 na underweight psima
  spread  raspodjela ishoda po seedovima (ne samo prosjek) za ključne scenarije
"""
from __future__ import annotations

import sys
from dataclasses import replace

from calorie_agent.config.config import CalorieAgentConfig

from . import harness, scenarios

BASE = CalorieAgentConfig()


def _runs(name: str, cfg: CalorieAgentConfig, seeds: int, **scenario_overrides):
    for seed in range(1, seeds + 1):
        sc = scenarios.ALL[name]()
        sc.seed = seed
        for key, value in scenario_overrides.items():
            setattr(sc, key, value)
        yield sc, harness.run(sc, cfg)


def noise(seeds: int) -> None:
    variants = {
        "bez adaptacije (sigma 0.15)": replace(BASE, noise_max_scale=1.0),
        "fiksno sigma 0.35": replace(BASE, weight_sigma_abs_kg=0.35, noise_max_scale=1.0),
        "adaptivno (trenutno)": BASE,
    }
    print(f"\n== procjena šuma vage ({seeds} seedova)")
    print(f"{'varijanta':<30} {'scenarij':<12} {'kraj min':>8} {'kraj max':>8} {'izvan raspona':>14} {'esc/pas':>8}")
    for label, cfg in variants.items():
        for name in ("noisy_scale", "obese", "secret_treats"):
            ends, out, esc = [], 0, 0
            for sc, tr in _runs(name, cfg, seeds):
                end = tr.weeks[-1].true_weight
                ends.append(end)
                out += end > sc.max_weight_kg or end < sc.min_weight_kg - 0.5
                esc += tr.count("ESCALATE")
            print(f"{label:<30} {name:<12} {min(ends):8.2f} {max(ends):8.2f} {out:>11}/{seeds} {esc / seeds:8.1f}")


def gain(seeds: int) -> None:
    print(f"\n== max_gain_pct_per_week ({seeds} seedova)")
    print(f"{'max_gain':>8} {'start kg':>8} {'medijan sedmica':>16} {'nikad':>8} {'iznad max':>10} {'max tjedni %':>13}")
    for start in (8.0, 6.5):
        for g in (1.0, 1.5, 2.0, 3.0):
            cfg = replace(BASE, max_gain_pct_per_week=g)
            arrive, over, peak = [], 0, 0.0
            for sc, tr in _runs("underweight", cfg, seeds, start_weight_kg=start):
                tw = [w.true_weight for w in tr.weeks]
                arrive.append(next((w.week for w in tr.weeks if w.true_weight >= sc.min_weight_kg), 99))
                over += max(tw) > sc.max_weight_kg
                peak = max(peak, max((b - a) / a * 100 for a, b in zip(tw, tw[1:])))
            median = sorted(arrive)[len(arrive) // 2]
            print(f"{g:>8} {start:>8} {'nikad' if median == 99 else median:>16} {sum(a == 99 for a in arrive):>5}/{seeds} "
                  f"{over:>7}/{seeds} {peak:13.2f}")


def spread(seeds: int) -> None:
    print(f"\n== raspodjela ishoda po seedu ({seeds} seedova), trenutni agent")
    print(f"{'scenarij':<14} {'kraj min':>8} {'kraj medijan':>13} {'kraj max':>8} {'izvan raspona':>14} {'esc/pas':>8}")
    for name in scenarios.ALL:
        ends, out, esc = [], 0, 0
        for sc, tr in _runs(name, BASE, seeds):
            end = tr.weeks[-1].true_weight
            ends.append(end)
            out += end > sc.max_weight_kg or end < sc.min_weight_kg
            esc += tr.count("ESCALATE")
        ends.sort()
        print(f"{name:<14} {ends[0]:8.2f} {ends[len(ends) // 2]:13.2f} {ends[-1]:8.2f} {out:>11}/{seeds} {esc / seeds:8.1f}")


def main() -> None:
    which = sys.argv[1] if len(sys.argv) > 1 else "all"
    seeds = int(sys.argv[2]) if len(sys.argv) > 2 else 20
    for key, fn in (("noise", noise), ("gain", gain), ("spread", spread)):
        if which in (key, "all"):
            fn(seeds)


if __name__ == "__main__":
    main()
