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
        return next((issue for issue in issues if issue.severity is not Severity.WARN), None)

    @classmethod
    def validate(cls, request: TickRequest, config: CalorieAgentConfig) -> tuple[ValidationIssue, ...]:
        return *cls._measurement(request, config), *cls._configuration(request, config), *cls._history(request, config)

    @classmethod
    def _finite(cls, *values: float) -> bool:
        return all(math.isfinite(value) for value in values)

    @classmethod
    def _measurement(cls, request: TickRequest, config: CalorieAgentConfig) -> list[ValidationIssue]:
        measurement, issues = request.measurement, []
        remeasure = Severity.REMEASURE
        if not cls._finite(measurement.weight, measurement.min_weight, measurement.max_weight):
            issues.append(ValidationIssue(remeasure, "measurement", "težina nije konačan broj"))
        elif measurement.weight <= 0:
            issues.append(ValidationIssue(remeasure, "measurement.weight", "težina mora biti veća od 0"))
        elif not config.weight_min_kg <= measurement.weight <= config.weight_max_kg:
            issues.append(ValidationIssue(
                remeasure, "measurement.weight",
                f"{measurement.weight:g} kg je izvan vjerodostojnog opsega "
                f"{config.weight_min_kg:g}-{config.weight_max_kg:g} kg",
                confirmable=True))
        elif 0 < measurement.min_weight < measurement.max_weight and not (
                measurement.min_weight / config.weight_vs_range_ratio
                <= measurement.weight
                <= measurement.max_weight * config.weight_vs_range_ratio):
            issues.append(ValidationIssue(
                remeasure, "measurement.weight",
                f"težina {measurement.weight:g} kg je predaleko od raspona "
                f"{measurement.min_weight:g}-{measurement.max_weight:g} kg",
                confirmable=True))
        if measurement.bcs not in config.valid_bcs:
            issues.append(ValidationIssue(remeasure, "measurement.bcs", f"BCS {measurement.bcs} nije u validnom opsegu"))
        return issues

    @classmethod
    def _configuration(cls, request: TickRequest, config: CalorieAgentConfig) -> list[ValidationIssue]:
        measurement, profile, intake, issues = request.measurement, request.profile, request.intake, []
        escalate = Severity.ESCALATE

        if cls._finite(measurement.min_weight, measurement.max_weight):
            if measurement.min_weight <= 0 or measurement.min_weight >= measurement.max_weight:
                issues.append(ValidationIssue(escalate, "measurement.minWeight/maxWeight",
                                              "raspon težine (MinWeight/MaxWeight)"))
            elif measurement.max_weight > config.range_max_ratio * measurement.min_weight:
                issues.append(ValidationIssue(
                    escalate, "measurement.minWeight/maxWeight",
                    f"raspon težine je prešir ({measurement.min_weight:g}-{measurement.max_weight:g} kg)"))

        food_kcal_per_gram = profile.food_kcal_per_gram
        if not (math.isfinite(food_kcal_per_gram)
                and config.food_kcal_min <= food_kcal_per_gram <= config.food_kcal_max):
            issues.append(ValidationIssue(escalate, "profile.foodKcalPerGram", "kcal po gramu hrane"))
        try:
            activity_factor = float(profile.activity_factor)
        except ValueError:
            activity_factor = math.nan
        if not (math.isfinite(activity_factor) and activity_factor in config.activity_factors):
            issues.append(ValidationIssue(escalate, "profile.activityFactor", "faktor aktivnosti"))

        # current_daily_kcal == None znači "plan još nema dozu" (prvi tick); to nije greška.
        plan = request.plan
        if plan is not None and plan.current_daily_kcal is not None and (
                not math.isfinite(plan.current_daily_kcal) or plan.current_daily_kcal <= 0):
            issues.append(ValidationIssue(escalate, "plan.currentDailyKcal", "trenutni dnevni plan"))

        if intake is not None:
            for name, grams in (("eatenGramsPerDay", intake.eaten_g_per_day),
                                ("offeredGramsPerDay", intake.offered_g_per_day)):
                if grams is not None and (not math.isfinite(grams) or grams < 0):
                    issues.append(ValidationIssue(escalate, f"intake.{name}", "unos hranilice"))
            if not (cls._finite(intake.period_days, intake.days_covered) and intake.period_days >= 0
                    and 0 <= intake.days_covered <= intake.period_days + config.days_covered_slack):
                issues.append(ValidationIssue(escalate, "intake.daysCovered/periodDays", "period unosa"))
            elif (intake.eaten_g_per_day is not None and intake.offered_g_per_day and intake.offered_g_per_day > 0
                  and intake.eaten_g_per_day > config.intake_over_offered_warn * intake.offered_g_per_day):
                issues.append(ValidationIssue(Severity.WARN, "intake.eatenGramsPerDay",
                                              "pojedeno je više od ponuđenog (poslastice ili greška senzora?)"))
        return issues

    @classmethod
    def _history(cls, request: TickRequest, config: CalorieAgentConfig) -> list[ValidationIssue]:
        issues, seen_timestamps = [], set()
        current_measured_at = request.measurement.measured_at
        for index, record in enumerate(request.history):
            if not (math.isfinite(record.weight_kg) and record.weight_kg > 0):
                issues.append(ValidationIssue(Severity.WARN, f"history[{index}].weightKg",
                                              "red se ignoriše: težina nije valjana"))
            if record.measured_at >= current_measured_at:
                issues.append(ValidationIssue(Severity.WARN, f"history[{index}].measuredAt",
                                              "red nije stariji od trenutnog mjerenja"))
            if record.measured_at in seen_timestamps:
                issues.append(ValidationIssue(Severity.WARN, f"history[{index}].measuredAt",
                                              "dupli datum u historiji"))
            seen_timestamps.add(record.measured_at)
            if record.bcs not in config.valid_bcs:
                issues.append(ValidationIssue(Severity.WARN, f"history[{index}].bcs",
                                              f"BCS {record.bcs} nije u validnom opsegu"))
        return issues

    @classmethod
    def intake_problem(cls, eaten_kcal: float, rer_kcal: float, config: CalorieAgentConfig) -> Optional[Severity]:
        """Odnos pojedenih kcal i RER-a: previsok (kvar senzora) daje WARN, prenizak (nevjerodostojno) ESCALATE."""
        if rer_kcal <= 0:
            return None
        eaten_to_rer_ratio = eaten_kcal / rer_kcal
        if eaten_to_rer_ratio > config.intake_rer_max:
            return Severity.WARN
        if eaten_to_rer_ratio < config.intake_rer_min:
            return Severity.ESCALATE
        return None
