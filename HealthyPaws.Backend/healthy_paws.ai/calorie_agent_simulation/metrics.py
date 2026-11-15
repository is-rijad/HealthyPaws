"""Tabela metrika po scenariju: `python -m calorie_agent_simulation.metrics`."""
from __future__ import annotations

from . import harness, scenarios


def summarize(tr: harness.Trace) -> dict:
    s, w = tr.scenario, tr.weeks
    plans = tr.plan_series()
    steps = [b - a for a, b in zip(plans, plans[1:]) if abs(b - a) > 1e-9]
    reversals = sum(1 for a, b in zip(steps, steps[1:]) if a * b < 0)
    in_range = next((x.week for x in w if s.min_weight_kg <= x.true_weight <= s.max_weight_kg), None)
    mer_err = [abs(x.mer_estimate - x.true_mer) / x.true_mer * 100 for x in w if x.mer_estimate]
    first_esc = tr.first_action("ESCALATE")
    return {
        "scenarij": s.name,
        "start_kg": round(w[0].true_weight, 2),
        "kraj_kg": round(w[-1].true_weight, 2),
        "ciljni_raspon": f"{s.min_weight_kg}-{s.max_weight_kg}",
        "sedmica_u_rasponu": in_range,
        "min_kg": round(min(x.true_weight for x in w), 2),
        "promjena_plana": len(steps),
        "obrata_plana": reversals,
        "eskalacija": tr.count("ESCALATE"),
        "prva_eskalacija": first_esc.week if first_esc else None,
        "trazeno_ponovno_vaganje": tr.remeasures,
        "mer_greska_kraj_%": round(mer_err[-1], 1) if mer_err else None,
    }


def main() -> None:
    rows = []
    for make in scenarios.ALL.values():
        sc = make()
        rows.append(summarize(harness.run(sc)))
    keys = list(rows[0])
    print(" | ".join(keys))
    for r in rows:
        print(" | ".join(str(r[k]) for k in keys))


if __name__ == "__main__":
    main()
