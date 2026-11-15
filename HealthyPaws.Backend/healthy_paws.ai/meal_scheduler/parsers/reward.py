from typing import Any

from meal_scheduler.data.reward_config import RewardConfig
from meal_scheduler.parsers.contract_error import ContractError
from meal_scheduler.parsers.json_parsers import req, num, opt_num


def compute_reward(outcome: Any, cfg: RewardConfig = RewardConfig()) -> float:
    """r = w_eaten * pojedeno/izdato + w_latency * (1 - latencija/prozor); bez zalogaja u prozoru latencijski dio je 0."""
    if not isinstance(outcome, dict):
        raise ContractError("'outcome' mora biti objekt")
    dispensed = num(req(outcome, "dispensedG"), "dispensedG")
    eaten = num(req(outcome, "eatenG"), "eatenG")
    if dispensed <= 0:
        raise ContractError("'dispensedG' mora biti > 0")
    if eaten < 0:
        raise ContractError("'eatenG' ne smije biti negativan")
    eaten_frac = min(eaten / dispensed, 1.0)
    lat = opt_num(outcome, "latencyS")
    if lat is None or lat < 0 or lat >= cfg.window_s:
        lat_term = 0.0
    else:
        lat_term = 1.0 - lat / cfg.window_s
    return cfg.w_eaten * eaten_frac + cfg.w_latency * lat_term
