from typing import Any

import math

import numpy as np

from meal_scheduler.constants import HOURS_SCALE, TREAT_SHARE_SCALE, MAX_MEAL_INDEX
from meal_scheduler.parsers.contract_error import ContractError
from meal_scheduler.parsers.json_parsers import opt_num


def build_features(ctx: Any) -> np.ndarray:
    if not isinstance(ctx, dict):
        raise ContractError("'context' mora biti objekt")
    hours = opt_num(ctx, "hoursSinceLastMeal")
    avg = opt_num(ctx, "avgEatenFraction")
    treat = opt_num(ctx, "treatBudgetShare")
    meal = ctx.get("mealIndex")
    if isinstance(meal, bool) or not isinstance(meal, int) or not 0 <= meal <= MAX_MEAL_INDEX:
        raise ContractError(f"'mealIndex' mora biti cijeli broj od 0 do {MAX_MEAL_INDEX}")
    for name, v in (("avgEatenFraction", avg), ("treatBudgetShare", treat)):
        if v is not None and not 0.0 <= v <= 1.0:
            raise ContractError(f"'{name}' mora biti u [0,1]")
    if hours is not None and hours < 0:
        raise ContractError("'hoursSinceLastMeal' ne smije biti negativan")
    hours_f = 0.0 if hours is None else min(hours / HOURS_SCALE, 1.0) - 0.5
    avg_f = 0.0 if avg is None else avg - 0.5
    treat_f = 0.0 if treat is None else min(treat / TREAT_SHARE_SCALE, 1.0) - 0.5
    return np.array([1.0, hours_f, avg_f, treat_f, float(meal == 1), float(meal == 2), float(meal == 3)])
