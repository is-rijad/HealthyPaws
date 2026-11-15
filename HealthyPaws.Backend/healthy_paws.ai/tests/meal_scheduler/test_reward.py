import pytest

from meal_scheduler.data.reward_config import RewardConfig, _window_s
from meal_scheduler.parsers.contract_error import ContractError
from meal_scheduler.parsers.reward import compute_reward

CFG = RewardConfig(w_eaten=0.6, w_latency=0.4, window_s=1800.0)


def reward(**outcome):
    return compute_reward({"dispensedG": 100, **outcome}, CFG)


class TestComputeReward:
    def test_everything_eaten_instantly_is_max(self):
        assert reward(eatenG=100, latencyS=0) == pytest.approx(1.0)

    def test_nothing_eaten_is_zero(self):
        assert reward(eatenG=0) == 0.0

    def test_eaten_fraction_only_when_no_latency(self):
        assert reward(eatenG=50) == pytest.approx(0.3)

    def test_latency_linear(self):
        assert reward(eatenG=100, latencyS=900) == pytest.approx(0.6 + 0.4 * 0.5)

    @pytest.mark.parametrize("lat", [1800, 2000, -5, None])
    def test_latency_outside_window_or_invalid_contributes_zero(self, lat):
        assert reward(eatenG=100, latencyS=lat) == pytest.approx(0.6)

    def test_eaten_more_than_dispensed_is_capped(self):
        assert reward(eatenG=250, latencyS=0) == pytest.approx(1.0)

    def test_monotonic_in_eaten(self):
        vals = [reward(eatenG=g, latencyS=300) for g in (0, 25, 50, 75, 100)]
        assert vals == sorted(vals)

    def test_monotonic_in_speed(self):
        vals = [reward(eatenG=100, latencyS=s) for s in (1700, 1200, 600, 0)]
        assert vals == sorted(vals)

    def test_range_is_unit_interval(self):
        for eaten in (0, 10, 100, 500):
            for lat in (None, 0, 100, 1799):
                assert 0.0 <= reward(eatenG=eaten, latencyS=lat) <= 1.0

    def test_custom_weights(self):
        cfg = RewardConfig(w_eaten=1.0, w_latency=0.0, window_s=60)
        assert compute_reward({"dispensedG": 10, "eatenG": 5, "latencyS": 0}, cfg) == pytest.approx(0.5)

    @pytest.mark.parametrize("outcome", [None, [], "x", 3])
    def test_outcome_must_be_object(self, outcome):
        with pytest.raises(ContractError, match="objekt"):
            compute_reward(outcome, CFG)

    @pytest.mark.parametrize("outcome", [
        {"eatenG": 1}, {"dispensedG": 10}, {"dispensedG": 0, "eatenG": 0}, {"dispensedG": -1, "eatenG": 0},
        {"dispensedG": 10, "eatenG": -1}, {"dispensedG": "10", "eatenG": 1}, {"dispensedG": 10, "eatenG": True},
        {"dispensedG": float("nan"), "eatenG": 1}, {"dispensedG": 10, "eatenG": 1, "latencyS": "x"},
    ])
    def test_invalid_outcomes(self, outcome):
        with pytest.raises(ContractError):
            compute_reward(outcome, CFG)


class TestWindowEnv:
    def test_default(self, monkeypatch):
        monkeypatch.delenv("BANDIT_REWARD_WINDOW_S", raising=False)
        assert _window_s() == 1800.0

    def test_from_env(self, monkeypatch):
        monkeypatch.setenv("BANDIT_REWARD_WINDOW_S", "600")
        assert _window_s() == 600.0
        assert RewardConfig().window_s == 600.0

    @pytest.mark.parametrize("v", ["abc", "0", "-5", ""])
    def test_invalid_falls_back(self, monkeypatch, v):
        monkeypatch.setenv("BANDIT_REWARD_WINDOW_S", v)
        assert _window_s() == 1800.0
