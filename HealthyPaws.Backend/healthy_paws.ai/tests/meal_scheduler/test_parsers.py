import numpy as np
import pytest

from meal_scheduler.constants import FEATURE_DIM, MAX_ARMS, MAX_OFFSET_MIN
from meal_scheduler.data.arm_state import ArmState
from meal_scheduler.data.lin_ucb_state import LinUCBState
from meal_scheduler.parsers.contract_error import ContractError
from meal_scheduler.parsers.features import build_features
from meal_scheduler.parsers.json_parsers import arm_id, num, opt_num, parse_arms, req
from meal_scheduler.parsers.state import state_from_json, state_to_json


class TestJsonParsers:
    def test_req_missing_or_null(self):
        with pytest.raises(ContractError, match="nedostaje"):
            req({}, "a")
        with pytest.raises(ContractError, match="nedostaje"):
            req({"a": None}, "a")

    def test_req_keeps_falsy_values(self):
        assert req({"a": 0}, "a") == 0
        assert req({"a": ""}, "a") == ""

    @pytest.mark.parametrize("v", [True, False, "1", None, [], float("nan"), float("inf")])
    def test_num_rejects(self, v):
        with pytest.raises(ContractError):
            num(v, "x")

    def test_num_accepts_int_and_float(self):
        assert num(3, "x") == 3.0 and isinstance(num(3, "x"), float)
        assert num(-1.5, "x") == -1.5

    def test_opt_num(self):
        assert opt_num({}, "k") is None
        assert opt_num({"k": None}, "k") is None
        assert opt_num({"k": 2}, "k") == 2.0
        with pytest.raises(ContractError):
            opt_num({"k": "2"}, "k")

    @pytest.mark.parametrize("v", [0, 15, -15, MAX_OFFSET_MIN, -MAX_OFFSET_MIN])
    def test_arm_id_valid(self, v):
        assert arm_id(v) == v

    @pytest.mark.parametrize("v", [MAX_OFFSET_MIN + 1, -MAX_OFFSET_MIN - 1, True, 1.5, "15", None])
    def test_arm_id_invalid(self, v):
        with pytest.raises(ContractError):
            arm_id(v)

    def test_parse_arms_ok(self):
        assert parse_arms({"arms": [0, 15, -15]}) == [0, 15, -15]

    @pytest.mark.parametrize("arms", [[], "0", {"0": 1}, list(range(MAX_ARMS + 1)), [0, 0], [0, 999], [0, True]])
    def test_parse_arms_invalid(self, arms):
        with pytest.raises(ContractError):
            parse_arms({"arms": arms})

    def test_parse_arms_max_allowed(self):
        assert len(parse_arms({"arms": list(range(MAX_ARMS))})) == MAX_ARMS

    def test_parse_arms_missing(self):
        with pytest.raises(ContractError):
            parse_arms({})


class TestFeatures:
    base = {"hoursSinceLastMeal": 6, "avgEatenFraction": 0.5, "treatBudgetShare": 0.1, "mealIndex": 0}

    def test_centered_values(self):
        x = build_features(self.base)
        assert x[0] == 1.0                      # bias
        assert x[1] == pytest.approx(0.0)       # 6h / 12 - 0.5
        assert x[2] == pytest.approx(0.0)       # 0.5 - 0.5
        assert x[3] == pytest.approx(0.0)       # 0.1 / 0.2 - 0.5

    def test_all_missing_optionals_default_to_zero(self):
        x = build_features({"mealIndex": 0})
        np.testing.assert_array_equal(x[:4], [1.0, 0.0, 0.0, 0.0])

    def test_hours_clipped_above_scale(self):
        assert build_features({**self.base, "hoursSinceLastMeal": 100})[1] == pytest.approx(0.5)
        assert build_features({**self.base, "hoursSinceLastMeal": 0})[1] == pytest.approx(-0.5)

    def test_treat_share_clipped(self):
        assert build_features({**self.base, "treatBudgetShare": 1.0})[3] == pytest.approx(0.5)

    @pytest.mark.parametrize("idx,expected", [(0, [0, 0, 0]), (1, [1, 0, 0]), (2, [0, 1, 0]), (3, [0, 0, 1])])
    def test_meal_index_one_hot_reference_category(self, idx, expected):
        x = build_features({**self.base, "mealIndex": idx})
        assert list(x[-3:]) == expected

    @pytest.mark.parametrize("meal", [-1, 4, 1.0, "1", True, None])
    def test_bad_meal_index(self, meal):
        with pytest.raises(ContractError, match="mealIndex"):
            build_features({**self.base, "mealIndex": meal})

    @pytest.mark.parametrize("field,value", [("avgEatenFraction", 1.1), ("avgEatenFraction", -0.1),
                                             ("treatBudgetShare", 1.5), ("treatBudgetShare", -0.01),
                                             ("hoursSinceLastMeal", -1)])
    def test_out_of_range(self, field, value):
        with pytest.raises(ContractError):
            build_features({**self.base, field: value})

    @pytest.mark.parametrize("ctx", [None, [], "x", 3])
    def test_context_must_be_object(self, ctx):
        with pytest.raises(ContractError, match="objekt"):
            build_features(ctx)

    def test_output_is_finite(self):
        assert np.all(np.isfinite(build_features(self.base)))


def _valid_arm(d=FEATURE_DIM, n=3):
    return {"A": np.eye(d).tolist(), "b": np.zeros(d).tolist(), "n": n}


class TestStateJson:
    def test_none_gives_fresh_state(self):
        s = state_from_json(None)
        assert (s.d, s.version, s.arms) == (FEATURE_DIM, 0, {})

    def test_roundtrip(self):
        A = np.eye(FEATURE_DIM) * 2.0
        A[0, 1] = A[1, 0] = 0.25
        original = LinUCBState(d=FEATURE_DIM, version=4,
                               arms={15: ArmState(A=A, b=np.arange(FEATURE_DIM, dtype=float), n=9),
                                     -15: ArmState(A=np.eye(FEATURE_DIM), b=np.ones(FEATURE_DIM), n=1)})
        back = state_from_json(state_to_json(original))
        assert back.version == 4 and set(back.arms) == {15, -15}
        np.testing.assert_allclose(back.arms[15].A, A)
        np.testing.assert_allclose(back.arms[15].b, np.arange(FEATURE_DIM))
        assert back.arms[15].n == 9

    def test_to_json_orders_arms_and_uses_string_keys(self):
        s = LinUCBState(d=FEATURE_DIM, arms={15: ArmState(np.eye(FEATURE_DIM), np.zeros(FEATURE_DIM)),
                                             -15: ArmState(np.eye(FEATURE_DIM), np.zeros(FEATURE_DIM))})
        assert list(state_to_json(s)["arms"]) == ["-15", "15"]

    def test_arms_default_to_empty(self):
        s = state_from_json({"d": FEATURE_DIM, "version": 2})
        assert s.arms == {} and s.version == 2

    @pytest.mark.parametrize("raw", ["x", [], 5])
    def test_state_must_be_object(self, raw):
        with pytest.raises(ContractError):
            state_from_json(raw)

    @pytest.mark.parametrize("d", [None, 3, FEATURE_DIM + 1, "8", True])
    def test_bad_dimension(self, d):
        with pytest.raises(ContractError, match="state.d"):
            state_from_json({"d": d, "version": 0})

    @pytest.mark.parametrize("v", [-1, "1", 1.5, True, None])
    def test_bad_version(self, v):
        with pytest.raises(ContractError, match="version"):
            state_from_json({"d": FEATURE_DIM, "version": v})

    def test_arms_must_be_object(self):
        with pytest.raises(ContractError, match="arms"):
            state_from_json({"d": FEATURE_DIM, "version": 0, "arms": []})

    def test_non_integer_arm_key(self):
        with pytest.raises(ContractError, match="ključ"):
            state_from_json({"d": FEATURE_DIM, "version": 0, "arms": {"abc": _valid_arm()}})

    def test_arm_key_out_of_range(self):
        with pytest.raises(ContractError):
            state_from_json({"d": FEATURE_DIM, "version": 0, "arms": {"999": _valid_arm()}})

    @pytest.mark.parametrize("arm", [{}, {"A": [[1]], "b": [0]}, {"A": "x", "b": "y"}, None, {"A": [[1, 2], [3]], "b": [1]}])
    def test_malformed_arm(self, arm):
        with pytest.raises(ContractError):
            state_from_json({"d": FEATURE_DIM, "version": 0, "arms": {"0": arm}})

    def test_nan_in_matrix_rejected(self):
        a = _valid_arm()
        a["A"][0][0] = float("nan")
        with pytest.raises(ContractError, match="NaN"):
            state_from_json({"d": FEATURE_DIM, "version": 0, "arms": {"0": a}})

    def test_asymmetric_matrix_rejected(self):
        a = _valid_arm()
        a["A"][0][1] = 0.5
        with pytest.raises(ContractError, match="simetri"):
            state_from_json({"d": FEATURE_DIM, "version": 0, "arms": {"0": a}})

    @pytest.mark.parametrize("n", [-1, 1.5, "3", True])
    def test_bad_arm_count(self, n):
        a = _valid_arm()
        a["n"] = n
        with pytest.raises(ContractError, match="'n'"):
            state_from_json({"d": FEATURE_DIM, "version": 0, "arms": {"0": a}})

    def test_missing_n_defaults_to_zero(self):
        a = _valid_arm()
        del a["n"]
        s = state_from_json({"d": FEATURE_DIM, "version": 0, "arms": {"0": a}})
        assert s.arms[0].n == 0
