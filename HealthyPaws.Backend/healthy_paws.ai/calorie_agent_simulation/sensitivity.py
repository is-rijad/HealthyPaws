"""Osjetljivost parametara: svaki se mijenja zasebno, prosjek preko seedova.

`python -m calorie_agent_simulation.sensitivity [broj_seedova]`
Služi kao dokaz za oznaku [SIM] u `calorie_agent/config/config.py`.
"""
from __future__ import annotations

import sys
from dataclasses import replace

from calorie_agent.config.config import CalorieAgentConfig

from . import harness, scenarios
from .metrics import summarize

SCENARIOS = ("obese", "secret_treats", "noisy_scale", "slow_drift")
SWEEPS: dict[str, tuple] = {
    "plan_smoothing": (0.25, 0.5, 0.75, 1.0),
    "min_change_pct": (0.0, 2.0, 4.0, 6.0, 8.0),
    "process_sd_kcal_per_week": (5.0, 10.0, 20.0, 40.0, 60.0),
    "trend_points": (4, 6, 8, 12),
}
BASE = CalorieAgentConfig()


def evaluate(cfg: CalorieAgentConfig, name: str, seeds: int) -> dict:
    rows = []
    for seed in range(1, seeds + 1):
        sc = scenarios.ALL[name](); sc.seed = seed
        tr = harness.run(sc, cfg)
        r = summarize(tr)
        # tjedana od početka događaja do prve eskalacije (None = nikad)
        start = min(sc.events) if sc.events else None
        esc = tr.first_action("ESCALATE", after_week=start - 1 if start is not None else -1)
        r["kasnjenje"] = (esc.week - start) if (esc and start is not None) else None
        rows.append(r)

    def avg(key):
        vals = [r[key] for r in rows if r[key] is not None]
        return sum(vals) / len(vals) if vals else float("nan")

    return {"promjena": avg("promjena_plana"), "obrata": avg("obrata_plana"), "kraj_kg": avg("kraj_kg"),
            "esc": avg("eskalacija"), "mer_err": avg("mer_greska_kraj_%"),
            "kasnjenje": avg("kasnjenje"),
            "ne_otkriveno": sum(r["kasnjenje"] is None for r in rows) if scenarios.ALL[name]().events else "-"}


def main(seeds: int = 8) -> None:
    for param, values in SWEEPS.items():
        print(f"\n== {param} (default {getattr(BASE, param)})")
        print(f"{'vrijednost':>10} {'scenarij':<14} {'promjena':>8} {'obrata':>6} {'kraj_kg':>7} {'esc':>5} "
              f"{'mer_err%':>8} {'kasnjenje':>9} {'ne_otkr.':>8}")
        for v in values:
            cfg = replace(BASE, **{param: v})
            for name in SCENARIOS:
                m = evaluate(cfg, name, seeds)
                print(f"{v:>10} {name:<14} {m['promjena']:8.1f} {m['obrata']:6.1f} {m['kraj_kg']:7.2f} "
                      f"{m['esc']:5.1f} {m['mer_err']:8.1f} {m['kasnjenje']:9.1f} {m['ne_otkriveno']:>8}")


if __name__ == "__main__":
    main(*(int(a) for a in sys.argv[1:2]))
