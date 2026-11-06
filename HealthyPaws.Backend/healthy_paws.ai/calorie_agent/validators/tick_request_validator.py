from __future__ import annotations

import math
from typing import Optional

from calorie_agent.config.config import CalorieAgentConfig
from calorie_agent.interfaces.intake_summary import IntakeSummary
from calorie_agent.interfaces.tick_request import TickRequest
from calorie_agent.validators.severity import Severity
from calorie_agent.validators.validation_issue import ValidationIssue


class TickRequestValidator:
    @classmethod
    def first_blocking(cls, issues: tuple[ValidationIssue, ...]) -> Optional[ValidationIssue]:
        return next((i for i in issues if i.severity is not Severity.WARN), None)

    @classmethod
    def validate(cls, req: TickRequest, cfg: CalorieAgentConfig) -> tuple[ValidationIssue, ...]:
        cls._req = req
        cls._cfg = cfg
        return *cls._measurement(), *cls._configuration(), *cls._history()

    @classmethod
    def _finite(cls, *xs: float) -> bool:
        return all(math.isfinite(x) for x in xs)

    @classmethod
    def _measurement(cls) -> list[ValidationIssue]:
        m, out = cls._req.measurement, []
        cfg = cls._cfg
        R = Severity.REMEASURE
        if not cls._finite(m.weight, m.min_weight, m.max_weight):
            out.append(ValidationIssue(R, "measurement", "težina nije konačan broj"))
        elif m.weight <= 0:
            out.append(ValidationIssue(R, "measurement.weight", "težina mora biti veća od 0"))
        elif not cfg.weight_min_kg <= m.weight <= cfg.weight_max_kg:
            out.append(ValidationIssue(R, "measurement.weight",
                             f"{m.weight:g} kg je izvan vjerodostojnog opsega {cfg.weight_min_kg:g}-{cfg.weight_max_kg:g} kg",
                             confirmable=True))
        elif 0 < m.min_weight < m.max_weight and not (
                m.min_weight / cfg.weight_vs_range_ratio <= m.weight <= m.max_weight * cfg.weight_vs_range_ratio):
            out.append(ValidationIssue(R, "measurement.weight",
                             f"težina {m.weight:g} kg je predaleko od raspona {m.min_weight:g}-{m.max_weight:g} kg",
                             confirmable=True))
        if m.bcs not in cfg.valid_bcs:
            out.append(ValidationIssue(R, "measurement.bcs", f"BCS {m.bcs} nije u validnom opsegu"))
        return out

    @classmethod
    def _configuration(cls) -> list[ValidationIssue]:
        req = cls._req
        cfg = cls._cfg

        m, p, it, out = req.measurement, req.profile, req.intake, []
        E = Severity.ESCALATE

        if cls._finite(m.min_weight, m.max_weight):
            if m.min_weight <= 0 or m.min_weight >= m.max_weight:
                out.append(ValidationIssue(E, "measurement.minWeight/maxWeight", "raspon težine (MinWeight/MaxWeight)"))
            else:
                if m.max_weight > cfg.range_max_ratio * m.min_weight:
                    out.append(ValidationIssue(E, "measurement.minWeight/maxWeight",
                                     f"raspon težine je prešir ({m.min_weight:g}-{m.max_weight:g} kg)"))

        if not (math.isfinite(p.food_kcal_per_gram) and cfg.food_kcal_min <= p.food_kcal_per_gram <= cfg.food_kcal_max):
            out.append(ValidationIssue(E, "profile.foodKcalPerGram", "kcal po gramu hrane"))
        if not (math.isfinite(float(p.activity_factor)) and float(p.activity_factor) in cfg.activity_factors):
            out.append(ValidationIssue(E, "profile.activityFactor", "faktor aktivnosti"))

        if req.plan is not None and (not math.isfinite(req.plan.current_daily_kcal) or req.plan.current_daily_kcal <= 0):
            out.append(ValidationIssue(E, "plan.currentDailyKcal", "trenutni dnevni plan"))

        if it is not None:
            for name, value in (("eatenGramsPerDay", it.eaten_g_per_day), ("offeredGramsPerDay", it.offered_g_per_day)):
                if value is not None and (not math.isfinite(value) or value < 0):
                    out.append(ValidationIssue(E, f"intake.{name}", "unos hranilice"))
            if not (cls._finite(it.period_days, it.days_covered) and it.period_days >= 0
                    and 0 <= it.days_covered <= it.period_days + cfg.days_covered_slack):
                out.append(ValidationIssue(E, "intake.daysCovered/periodDays", "period unosa"))
            elif (it.eaten_g_per_day is not None and it.offered_g_per_day and it.offered_g_per_day > 0
                  and it.eaten_g_per_day > cfg.intake_over_offered_warn * it.offered_g_per_day):
                out.append(ValidationIssue(Severity.WARN, "intake.eatenGramsPerDay",
                                 "pojedeno je više od ponuđenog (poslastice ili greška senzora?)"))
        return out

    @classmethod
    def _history(cls) -> list[ValidationIssue]:
        req = cls._req
        cfg = cls._cfg

        out, seen = [], set()
        now = req.measurement.measured_at
        for i, r in enumerate(req.history):
            if not (math.isfinite(r.weight_kg) and r.weight_kg > 0):
                out.append(ValidationIssue(Severity.WARN, f"history[{i}].weightKg", "red se ignoriše: težina nije valjana"))
            if r.measured_at >= now:
                out.append(ValidationIssue(Severity.WARN, f"history[{i}].measuredAt", "red nije stariji od trenutnog mjerenja"))
            if r.measured_at in seen:
                out.append(ValidationIssue(Severity.WARN, f"history[{i}].measuredAt", "dupli datum u historiji"))
            seen.add(r.measured_at)
            if r.bcs not in cfg.valid_bcs:
                out.append(ValidationIssue(Severity.WARN, f"history[{i}].bcs", f"BCS {r.bcs} nije u validnom opsegu"))
        return out

    @classmethod
    def intake_problem(cls, eaten_kcal: float, rer_kcal: float) -> Optional[
        Severity]:
        cfg = cls._cfg

        if rer_kcal <= 0:
            return None
        ratio = eaten_kcal / rer_kcal
        if ratio > cfg.intake_rer_max:
            return Severity.WARN
        if ratio < cfg.intake_rer_min:
            return Severity.ESCALATE
        return None