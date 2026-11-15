import math
from typing import Mapping, Any

from meal_scheduler.constants import MAX_OFFSET_MIN, MAX_ARMS
from meal_scheduler.parsers.contract_error import ContractError


def req(obj: Mapping[str, Any], key: str) -> Any:
    if key not in obj or obj[key] is None:
        raise ContractError(f"nedostaje polje '{key}'")
    return obj[key]


def num(v: Any, name: str) -> float:
    if isinstance(v, bool) or not isinstance(v, (int, float)) or not math.isfinite(v):
        raise ContractError(f"'{name}' mora biti konačan broj")
    return float(v)


def opt_num(obj: Mapping[str, Any], key: str) -> float | None:
    v = obj.get(key)
    return None if v is None else num(v, key)


def arm_id(v: Any) -> int:
    if isinstance(v, bool) or not isinstance(v, int):
        raise ContractError("arm mora biti cijeli broj (pomak u minutama)")
    if abs(v) > MAX_OFFSET_MIN:
        raise ContractError(f"arm {v} izvan dozvoljenog raspona ±{MAX_OFFSET_MIN} min")
    return v


def parse_arms(obj: Mapping[str, Any]) -> list[int]:
    raw = req(obj, "arms")
    if not isinstance(raw, list) or not 1 <= len(raw) <= MAX_ARMS:
        raise ContractError(f"'arms' mora biti lista od 1 do {MAX_ARMS} elemenata")
    arms = [arm_id(a) for a in raw]
    if len(set(arms)) != len(arms):
        raise ContractError("'arms' sadrži duplikate")
    return arms
