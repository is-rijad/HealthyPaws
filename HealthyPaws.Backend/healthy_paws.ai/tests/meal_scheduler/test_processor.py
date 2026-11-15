"""End-to-end test JSON ugovora između backenda (.NET) i LinUCB-a."""
import json

import pytest

from meal_scheduler.constants import FEATURE_DIM, MAX_OFFSET_MIN
from meal_scheduler.data.lin_ucb_config import LinUCBConfig
from meal_scheduler.parsers.contract_error import ContractError
from meal_scheduler.processor import process_feedback, process_select

CTX = {"hoursSinceLastMeal": 4, "avgEatenFraction": 0.8, "treatBudgetShare": 0.05, "mealIndex": 1}
OUTCOME = {"dispensedG": 100, "eatenG": 90, "latencyS": 120}


def body(**kw) -> bytes:
    return json.dumps(kw).encode()


def select_body(**over):
    return body(**{"decisionId": "d1", "dogId": 7, "arms": [-15, 0, 15], "context": CTX, **over})


def feedback_body(**over):
    return body(**{"decisionId": "d1", "dogId": 7, "arm": 15, "context": CTX, "outcome": OUTCOME, **over})


class TestSelect:
    def test_response_shape(self):
        out = json.loads(process_select(select_body()))
        assert out["decisionId"] == "d1" and out["dogId"] == "7"  # dogId se uvijek vraća kao string
        assert out["arm"] in (-15, 0, 15)
        assert set(out["scores"]) == {"-15", "0", "15"}
        assert out["stateVersion"] == 0

    def test_fresh_state_picks_zero_offset(self):
        assert json.loads(process_select(select_body()))["arm"] == 0

    def test_state_version_is_echoed(self):
        fb = json.loads(process_feedback(feedback_body()))
        sel = json.loads(process_select(select_body(state=fb["state"])))
        assert sel["stateVersion"] == 1

    def test_learns_from_feedback_and_changes_choice(self):
        state = None
        for _ in range(15):
            fb = json.loads(process_feedback(feedback_body(arm=15, state=state)))
            state = fb["state"]
            fb = json.loads(process_feedback(feedback_body(arm=0, state=state, outcome={"dispensedG": 100, "eatenG": 0})))
            state = fb["state"]
        cfg = LinUCBConfig(alpha=0.05)
        assert json.loads(process_select(select_body(arms=[0, 15], state=state), cfg))["arm"] == 15

    def test_accepts_numeric_ids(self):
        out = json.loads(process_select(select_body(decisionId=42, dogId=3)))
        assert out["decisionId"] == "42" and out["dogId"] == "3"

    def test_output_is_compact_valid_json_without_nan(self):
        raw = process_select(select_body())
        assert b" " not in raw and b"NaN" not in raw

    @pytest.mark.parametrize("raw", [b"", b"{", b"not json", b"\xff\xfe", b"[]", b"1", b'"x"', b"null"])
    def test_malformed_body(self, raw):
        with pytest.raises(ContractError):
            process_select(raw)

    @pytest.mark.parametrize("const", [b"NaN", b"Infinity", b"-Infinity"])
    def test_json_constants_rejected(self, const):
        raw = b'{"decisionId":"a","dogId":1,"arms":[0],"context":{"mealIndex":0,"hoursSinceLastMeal":' + const + b"}}"
        with pytest.raises(ContractError, match="konstanta"):
            process_select(raw)

    @pytest.mark.parametrize("missing", ["decisionId", "dogId", "arms", "context"])
    def test_missing_required_field(self, missing):
        payload = json.loads(select_body())
        del payload[missing]
        with pytest.raises(ContractError):
            process_select(json.dumps(payload).encode())

    @pytest.mark.parametrize("bad", ["", True, [], {}, 1.5])
    def test_bad_ids(self, bad):
        with pytest.raises(ContractError):
            process_select(select_body(decisionId=bad))
        with pytest.raises(ContractError):
            process_select(select_body(dogId=bad))

    @pytest.mark.parametrize("arms", [[], [0, 0], [MAX_OFFSET_MIN + 1], "0"])
    def test_bad_arms(self, arms):
        with pytest.raises(ContractError):
            process_select(select_body(arms=arms))

    def test_bad_state_is_contract_error(self):
        with pytest.raises(ContractError):
            process_select(select_body(state={"d": 3, "version": 0}))

    def test_bad_context_is_contract_error(self):
        with pytest.raises(ContractError):
            process_select(select_body(context={"mealIndex": 9}))


class TestFeedback:
    def test_response_has_reward_and_new_state(self):
        out = json.loads(process_feedback(feedback_body()))
        assert out["decisionId"] == "d1" and out["dogId"] == "7"
        assert out["reward"] == pytest.approx(0.6 * 0.9 + 0.4 * (1 - 120 / 1800))
        assert out["state"]["version"] == 1 and out["state"]["d"] == FEATURE_DIM
        assert out["state"]["arms"]["15"]["n"] == 1

    def test_state_chains_across_feedbacks(self):
        s1 = json.loads(process_feedback(feedback_body()))["state"]
        s2 = json.loads(process_feedback(feedback_body(state=s1)))["state"]
        assert s2["version"] == 2 and s2["arms"]["15"]["n"] == 2

    def test_state_output_is_reparsable(self):
        s = json.loads(process_feedback(feedback_body()))["state"]
        process_feedback(feedback_body(arm=0, state=s))  # ne smije baciti

    def test_other_arms_untouched(self):
        s1 = json.loads(process_feedback(feedback_body(arm=15)))["state"]
        s2 = json.loads(process_feedback(feedback_body(arm=0, state=s1)))["state"]
        assert s2["arms"]["15"] == s1["arms"]["15"]

    @pytest.mark.parametrize("missing", ["decisionId", "dogId", "arm", "context", "outcome"])
    def test_missing_required_field(self, missing):
        payload = json.loads(feedback_body())
        del payload[missing]
        with pytest.raises(ContractError):
            process_feedback(json.dumps(payload).encode())

    @pytest.mark.parametrize("arm", [1.5, "15", True, 61, -61])
    def test_bad_arm(self, arm):
        with pytest.raises(ContractError):
            process_feedback(feedback_body(arm=arm))

    @pytest.mark.parametrize("outcome", [{}, {"dispensedG": 0, "eatenG": 0}, {"dispensedG": 10, "eatenG": -1}, "x"])
    def test_bad_outcome(self, outcome):
        with pytest.raises(ContractError):
            process_feedback(feedback_body(outcome=outcome))

    def test_malformed_body(self):
        with pytest.raises(ContractError):
            process_feedback(b"{")
