"""Generiše PNG grafikone po scenariju (za diplomski): `python -m calorie_agent_simulation.report [izlazni_dir]`.

Dva panela po scenariju (bez dvostruke ose): gore težina psa, dolje dnevni plan u kcal.
Plava = istinita težina / plan, narandžasta = granice i događaji; markeri nose oblik pa boja nije jedini nosilac.
"""
from __future__ import annotations

import sys
import textwrap
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

from . import harness, scenarios

BLUE, ORANGE = "#2a78d6", "#eb6834"
INK, MUTED, GRID, SURFACE = "#0b0b0b", "#52514e", "#e6e5e1", "#fcfcfb"


def plot(tr: harness.Trace, out: Path) -> None:
    s = tr.scenario
    wk = [w.week for w in tr.weeks]
    fig, (a1, a2) = plt.subplots(2, 1, figsize=(8, 6), sharex=True, facecolor=SURFACE,
                                 gridspec_kw={"height_ratios": [3, 2], "hspace": 0.12})
    for a in (a1, a2):
        a.set_facecolor(SURFACE)
        a.grid(axis="y", color=GRID, lw=0.8)
        for sp in ("top", "right", "left"):
            a.spines[sp].set_visible(False)
        a.spines["bottom"].set_color(GRID)
        a.tick_params(colors=MUTED, length=0)

    a1.axhspan(s.min_weight_kg, s.max_weight_kg, color=ORANGE, alpha=0.10, lw=0)
    a1.plot(wk, [w.true_weight for w in tr.weeks], color=BLUE, lw=2, label="istinita težina psa")
    a1.plot(wk, [w.reported_weight for w in tr.weeks], color=MUTED, lw=0, marker=".", ms=5, label="izmjereno (šum vage)")
    esc = [w for w in tr.weeks if w.action == "ESCALATE"]
    a1.plot([w.week for w in esc], [w.true_weight for w in esc], color=ORANGE, lw=0, marker="D", ms=8,
            mec=SURFACE, mew=2, label="eskalacija (pregled)")
    rem = [w for w in tr.weeks if w.action == "REQUEST_REMEASURE"]
    a1.plot([w.week for w in rem], [w.true_weight for w in rem], color=ORANGE, lw=0, marker="^", ms=8,
            mec=SURFACE, mew=2, label="traži ponovno vaganje")
    a1.set_ylabel("kg", color=MUTED)
    a1.set_title(textwrap.fill(f"{s.name}: {s.description}", 80), loc="left", color=INK, fontsize=11)
    a1.legend(frameon=False, fontsize=8, labelcolor=MUTED, loc="best", ncol=2)
    a1.text(0.01, 0.02, "narandžasta traka = zdravi raspon", transform=a1.transAxes, color=MUTED, fontsize=8)

    pk = [(w.week, w.plan_kcal) for w in tr.weeks if w.plan_kcal]
    a2.step([p[0] for p in pk], [p[1] for p in pk], where="post", color=BLUE, lw=2, label="plan (kcal/dan)")
    fl = [(w.week, w.floor_kcal) for w in tr.weeks if w.floor_kcal]
    a2.plot([p[0] for p in fl], [p[1] for p in fl], color=ORANGE, lw=1.5, ls="--", label="donja sigurnosna granica")
    a2.set_ylabel("kcal/dan", color=MUTED)
    a2.set_xlabel("sedmica", color=MUTED)
    a2.legend(frameon=False, fontsize=8, labelcolor=MUTED, loc="best")
    fig.savefig(out, dpi=150, bbox_inches="tight", facecolor=SURFACE)
    plt.close(fig)


def main(out_dir: str = "report") -> None:
    d = Path(out_dir); d.mkdir(parents=True, exist_ok=True)
    for name, make in scenarios.ALL.items():
        sc = make(); sc.seed = 1
        plot(harness.run(sc), d / f"{name}.png")
        print("napisano", d / f"{name}.png")


if __name__ == "__main__":
    main(*sys.argv[1:2])
