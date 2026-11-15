"""Ugovor C# <-> Python. Golden fajlovi su zajednički sa C# testovima (HealthyPaws.Backend/contracts/bandit)."""
import json
from pathlib import Path

import pytest

from meal_scheduler.constants import FEATURE_DIM
from meal_scheduler.processor import process_feedback, process_select

CONTRACTS = Path(__file__).resolve().parents[3] / "contracts" / "bandit"

def load(name: str) -> dict:
    return json.loads((CONTRACTS / name).read_text())


def with_dim(payload: dict) -> dict:
    return payload


def test_python_accepts_csharp_select_request():
    out = json.loads(process_select(json.dumps(with_dim(load("select_request.json"))).encode()))
    assert set(out) == set(load("select_response.json"))
    assert out["arm"] in load("select_request.json")["arms"]


def test_python_accepts_csharp_select_request_with_state():
    out = json.loads(process_select(json.dumps(with_dim(load("select_request_with_state.json"))).encode()))
    assert out["stateVersion"] == 3


def test_python_accepts_csharp_feedback_request():
    out = json.loads(process_feedback(json.dumps(with_dim(load("feedback_request.json"))).encode()))
    assert set(out) == set(load("feedback_response.json"))
    assert out["state"]["version"] == 4  # version iz zahtjeva (3) + 1 -> C# consumer očekuje version-1 u bazi


def test_csharp_requests_contain_all_keys_python_reads():
    context = load("select_request.json")["context"]
    assert {"hoursSinceLastMeal", "avgEatenFraction", "treatBudgetShare", "mealIndex"} <= set(context)


def test_csharp_context_has_meal_index():
    assert "mealIndex" in load("select_request.json")["context"]


def test_golden_state_has_production_dimension():
    assert load("feedback_response.json")["state"]["d"] == FEATURE_DIM == load("select_request_with_state.json")["state"]["d"]


def test_golden_response_shapes_are_what_csharp_deserializes():
    select, feedback = load("select_response.json"), load("feedback_response.json")
    assert set(select) == {"decisionId", "dogId", "arm", "scores", "stateVersion"}
    assert set(feedback) == {"decisionId", "dogId", "reward", "state"}
    assert isinstance(select["dogId"], str) and isinstance(feedback["dogId"], str)  # C# ima string DogId
    assert 0.0 <= feedback["reward"] <= 1.0


def test_arms_in_golden_request_are_valid_for_python():
    from meal_scheduler.constants import MAX_ARMS, MAX_OFFSET_MIN
    arms = load("select_request.json")["arms"]
    assert 1 <= len(arms) <= MAX_ARMS and all(abs(a) <= MAX_OFFSET_MIN for a in arms)
