import json
from typing import Mapping, Any

from meal_scheduler.data.lin_ucb_config import LinUCBConfig
from meal_scheduler.data.reward_config import RewardConfig
from meal_scheduler.lin_ucb import LinUcb
from meal_scheduler.parsers.contract_error import ContractError
from meal_scheduler.parsers.features import build_features
from meal_scheduler.parsers.json_parsers import req, parse_arms, arm_id
from meal_scheduler.parsers.reward import compute_reward
from meal_scheduler.parsers.state import state_from_json, state_to_json


def _dump(obj: dict) -> bytes:
    return json.dumps(obj, separators=(",", ":"), allow_nan=False).encode("utf-8")

def _bad_const(name: str):
    raise ContractError(f"nedozvoljena JSON konstanta: {name}")


def _loads(body: bytes) -> dict:
    try:
        obj = json.loads(body, parse_constant=_bad_const)
    except ContractError:
        raise
    except (ValueError, UnicodeDecodeError) as e:
        raise ContractError(f"nevaljan JSON: {e}") from e
    if not isinstance(obj, dict):
        raise ContractError("korijen poruke mora biti objekt")
    return obj

def _str(obj: Mapping[str, Any], key: str) -> str:
    v = req(obj, key)
    if isinstance(v, bool) or not isinstance(v, (str, int)) or str(v) == "":
        raise ContractError(f"'{key}' mora biti neprazan string ili cijeli broj")
    return str(v)

def process_select(body: bytes, cfg: LinUCBConfig = LinUCBConfig()) -> bytes:
    request = _loads(body)
    decision_id, dog_id = _str(request, "decisionId"), _str(request, "dogId")
    arms = parse_arms(request)
    x = build_features(req(request, "context"))
    state = state_from_json(request.get("state"))
    arm, sc = LinUcb.select(state, x, arms, cfg)
    return _dump({
        "decisionId": decision_id,
        "dogId": dog_id,
        "arm": arm,
        "scores": {str(a): round(v, 9) for a, v in sc.items()},
        "stateVersion": state.version,
    })


def process_feedback(body: bytes, cfg: LinUCBConfig = LinUCBConfig(),
                     rcfg: RewardConfig = RewardConfig()) -> bytes:
    request = _loads(body)
    decision_id, dog_id = _str(request, "decisionId"), _str(request, "dogId")
    arm = arm_id(req(request, "arm"))
    x = build_features(req(request, "context"))
    reward = compute_reward(req(request, "outcome"), rcfg)
    state = state_from_json(request.get("state"))
    new_state = LinUcb.update(state, arm, x, reward, cfg)
    return _dump({
        "decisionId": decision_id,
        "dogId": dog_id,
        "reward": reward,
        "state": state_to_json(new_state),
    })