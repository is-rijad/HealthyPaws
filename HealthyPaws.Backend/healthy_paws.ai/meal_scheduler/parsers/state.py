from typing import Any

import numpy as np

from meal_scheduler.constants import FEATURE_DIM
from meal_scheduler.data.arm_state import ArmState
from meal_scheduler.data.lin_ucb_state import LinUCBState
from meal_scheduler.parsers.contract_error import ContractError
from meal_scheduler.parsers.json_parsers import arm_id


def state_from_json(raw: Any) -> LinUCBState:
    if raw is None:
        return LinUCBState.new(FEATURE_DIM)
    if not isinstance(raw, dict):
        raise ContractError("'state' mora biti objekt ili null")
    d = raw.get("d")
    if d != FEATURE_DIM or isinstance(d, bool):
        raise ContractError(f"'state.d' mora biti {FEATURE_DIM}")
    version = raw.get("version")
    if isinstance(version, bool) or not isinstance(version, int) or version < 0:
        raise ContractError("'state.version' mora biti cijeli broj >= 0")
    raw_arms = raw.get("arms", {})
    if not isinstance(raw_arms, dict):
        raise ContractError("'state.arms' mora biti objekt")
    arms: dict[int, ArmState] = {}
    for k, v in raw_arms.items():
        try:
            arm = arm_id(int(k))
        except ValueError:
            raise ContractError(f"ključ armova '{k}' nije cijeli broj") from None
        try:
            A = np.array(v["A"], dtype=float)
            b = np.array(v["b"], dtype=float)
            n = v.get("n", 0)
        except (KeyError, TypeError, ValueError):
            raise ContractError(f"arm {k}: nevaljan 'A'/'b'") from None
        if A.shape != (d, d) or b.shape != (d,):
            raise ContractError(f"arm {k}: A mora biti {d}x{d}, b dužine {d}")
        if not (np.all(np.isfinite(A)) and np.all(np.isfinite(b))):
            raise ContractError(f"arm {k}: A/b sadrže NaN/inf")
        if not np.allclose(A, A.T, rtol=1e-9, atol=1e-9):
            raise ContractError(f"arm {k}: A nije simetrična")
        if isinstance(n, bool) or not isinstance(n, int) or n < 0:
            raise ContractError(f"arm {k}: 'n' mora biti cijeli broj >= 0")
        arms[arm] = ArmState(A=A, b=b, n=n)
    return LinUCBState(d=d, version=version, arms=arms)


def state_to_json(state: LinUCBState) -> dict:
    return {
        "d": state.d,
        "version": state.version,
        "arms": {
            str(a): {"A": s.A.tolist(), "b": s.b.tolist(), "n": s.n}
            for a, s in sorted((state.arms or {}).items())
        },
    }
