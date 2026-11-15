import math
from datetime import datetime, timezone
from types import SimpleNamespace

import pytest

from calorie_agent.config.config import CalorieAgentConfig
from calorie_agent.core.calculations import Calculations
from calorie_agent.core.classification.body_state import BodyState
from calorie_agent.core.classification.classification import Classification
from calorie_agent.core.controller import Controller
from calorie_agent.core.estimator import Estimator
from calorie_agent.core.explanations import Explanations
from calorie_agent.core.guardrails import GuardRails
from calorie_agent.core.trend import fit
from calorie_agent.interfaces.dog_profile import DogProfile
from calorie_agent.interfaces.learned_state import LearnedState

CFG = CalorieAgentConfig()


def profile(neutered=True, activity="1.2"):
    return DogProfile("adult", neutered, activity, False, 3.5)


class TestCalculations:
    calc = Calculations(CFG)

    def test_rer_known_value(self):
        assert self.calc.rer(10) == pytest.approx(70 * 10 ** 0.75)
        assert self.calc.rer(10) == pytest.approx(393.64, abs=0.01)

    def test_rer_monotonic(self):
        assert self.calc.rer(5) < self.calc.rer(10) < self.calc.rer(40)

    def test_mer_prior_neutered_vs_intact(self):
        neutered, intact = self.calc.mer_prior(20, profile(True)), self.calc.mer_prior(20, profile(False))
        assert neutered == pytest.approx(self.calc.rer(20) * 1.6 * 1.2)
        assert intact == pytest.approx(self.calc.rer(20) * 1.8 * 1.2)
        assert intact > neutered

    def test_kcal_bounds_use_lighter_for_floor_heavier_for_ceiling(self):
        floor, ceiling = self.calc.kcal_bounds(30, 20)
        assert floor == pytest.approx(self.calc.rer(20) * 1.0)
        assert ceiling == pytest.approx(self.calc.rer(30) * 3.0)
        f2, c2 = self.calc.kcal_bounds(20, 30)
        assert f2 == pytest.approx(floor) and c2 == pytest.approx(ceiling)

    def test_weight_sigma_combines_abs_and_relative(self):
        assert self.calc.weight_sigma(20) == pytest.approx(math.hypot(0.15, 0.005 * 20))
        assert self.calc.weight_sigma(80) > self.calc.weight_sigma(5) > 0.15 - 1e-12

    @pytest.mark.parametrize("bcs,expected", [(None, 0), (1, -1), (3, -1), (4, 0), (5, 0), (6, 0), (7, 1), (9, 1)])
    def test_bcs_direction(self, bcs, expected):
        assert self.calc.bcs_direction(bcs) == expected


class TestTrend:
    SIGMA = 0.15

    def test_fewer_than_three_points_is_none(self):
        assert fit([], self.SIGMA) is None
        assert fit([(0, 10), (1, 10)], self.SIGMA) is None

    def test_all_same_time_is_none(self):
        assert fit([(0, 10), (0, 11), (0, 12)], self.SIGMA) is None

    def test_perfect_line(self):
        t = fit([(-2, 22.0), (-1, 21.0), (0, 20.0)], self.SIGMA)
        assert t.slope_kg_per_week == pytest.approx(-1.0) and t.n == 3

    def test_flat(self):
        assert fit([(-2, 20), (-1, 20), (0, 20)], self.SIGMA).slope_kg_per_week == pytest.approx(0.0)

    def test_standard_error_formula_and_shrinks_with_more_points(self):
        three = fit([(-2, 20), (-1, 20), (0, 20)], self.SIGMA)
        assert three.se_kg_per_week == pytest.approx(self.SIGMA / math.sqrt(2.0))  # sum (t-mean)^2 = 2
        eight = fit([(-7 + i, 20) for i in range(8)], self.SIGMA)
        assert eight.se_kg_per_week < three.se_kg_per_week

    def test_unequal_spacing(self):
        t = fit([(-4, 24.0), (-1, 21.0), (0, 20.0)], self.SIGMA)
        assert t.slope_kg_per_week == pytest.approx(-1.0)


class _EstimatorAgent(SimpleNamespace):
    pass


def make_estimator(config=CFG):
    return Estimator(_EstimatorAgent(config=config))


class TestEstimator:
    est = make_estimator()

    def test_init_learned_sets_prior_and_variance(self):
        learned = self.est.init_learned(1000.0, LearnedState(n_updates=2))
        assert learned.mer_estimate_kcal == 1000.0
        assert learned.variance_kcal2 == pytest.approx((0.2 * 1000) ** 2)
        assert learned.n_updates == 2  # ostala polja se čuvaju

    def test_innovation_energy_balance(self):
        learned = LearnedState(mer_estimate_kcal=1000.0, variance_kcal2=10_000.0)
        # pojede 1100, gubi 0.1 kg u 7 dana -> observed = 1100 + 7000*0.1/7 = 1200
        inn = self.est.innovation(learned, 1100.0, 20.0, 19.9, 7.0, 0.15)
        assert inn.observed_mer == pytest.approx(1200.0)
        assert inn.innovation_kcal == pytest.approx(200.0)
        assert inn.actual_weight_change_kg == pytest.approx(-0.1)
        assert inn.predicted_weight_change_kg == pytest.approx((1100 - 1000) * 7 / 7000)

    def test_gaining_weight_lowers_observed_mer(self):
        learned = LearnedState(mer_estimate_kcal=1000.0, variance_kcal2=10_000.0)
        assert self.est.innovation(learned, 1000.0, 20.0, 20.2, 7.0, 0.15).observed_mer < 1000.0

    def test_variances(self):
        learned = LearnedState(mer_estimate_kcal=1000.0, variance_kcal2=10_000.0)
        inn = self.est.innovation(learned, 1000.0, 20.0, 20.0, 14.0, 0.15)
        assert inn.predicted_variance == pytest.approx(10_000 + 20.0 ** 2 * 2.0)
        assert inn.observation_variance == pytest.approx((7000 * math.sqrt(2) * 0.15 / 14.0) ** 2)

    def test_longer_interval_means_less_observation_noise(self):
        learned = LearnedState(mer_estimate_kcal=1000.0, variance_kcal2=10_000.0)
        short = self.est.innovation(learned, 1000.0, 20.0, 20.0, 7.0, 0.15)
        long_ = self.est.innovation(learned, 1000.0, 20.0, 20.0, 28.0, 0.15)
        assert long_.observation_variance < short.observation_variance

    def test_z_score_zero_when_observation_matches_estimate(self):
        learned = LearnedState(mer_estimate_kcal=1000.0, variance_kcal2=10_000.0)
        assert self.est.innovation(learned, 1000.0, 20.0, 20.0, 7.0, 0.15).z_score == pytest.approx(0.0)

    def test_apply_update_moves_estimate_toward_observation_and_shrinks_variance(self):
        learned = LearnedState(mer_estimate_kcal=1000.0, variance_kcal2=10_000.0, consecutive_bad_weeks=2)
        inn = self.est.innovation(learned, 1100.0, 20.0, 19.9, 7.0, 0.15)
        when = datetime(2026, 1, 8, tzinfo=timezone.utc)
        new = self.est.apply_update(learned, inn, when)
        assert 1000.0 < new.mer_estimate_kcal < inn.observed_mer
        total = inn.predicted_variance + inn.observation_variance
        gain = inn.predicted_variance / total
        assert new.mer_estimate_kcal == pytest.approx(1000.0 + gain * 200.0)
        assert new.variance_kcal2 == pytest.approx((1 - gain) * inn.predicted_variance)
        assert new.variance_kcal2 < inn.predicted_variance
        assert (new.n_updates, new.consecutive_bad_weeks, new.last_update_at) == (1, 0, when)

    def test_estimate_converges_to_true_mer_over_many_updates(self):
        """Simulacija: pas troši 1300 kcal, jede 1300 -> težina stabilna; procjena (prior 1000) mora konvergirati."""
        learned = LearnedState(mer_estimate_kcal=1000.0, variance_kcal2=(0.2 * 1000) ** 2)
        true_mer, eaten, weight = 1300.0, 1400.0, 20.0
        when = datetime(2026, 1, 1, tzinfo=timezone.utc)
        for _ in range(30):
            new_weight = weight + (eaten - true_mer) * 7 / 7000.0  # bez šuma
            inn = self.est.innovation(learned, eaten, weight, new_weight, 7.0, 0.15)
            learned = self.est.apply_update(learned, inn, when)
            weight = new_weight
        assert learned.mer_estimate_kcal == pytest.approx(true_mer, rel=0.02)


def make_guard(config=CFG, food_kcal_uncertainty=0.0):
    return GuardRails(SimpleNamespace(config=config, profile=DogProfile("adult", True, "1.2", False, 3.5, food_kcal_uncertainty)))


class TestGuardRails:
    g = make_guard()

    def test_step_limit_warmup_vs_normal(self):
        assert self.g.step_limit_pct(0, False) == 5.0
        assert self.g.step_limit_pct(2, False) == 5.0
        assert self.g.step_limit_pct(3, False) == 10.0

    def test_step_limit_conflict_halves(self):
        assert self.g.step_limit_pct(0, True) == 2.5
        assert self.g.step_limit_pct(10, True) == 5.0

    def test_estimated_food_kcal_halves_step(self):
        g = make_guard(food_kcal_uncertainty=0.15)
        assert g.step_limit_pct(0, False) == 2.5
        assert g.step_limit_pct(3, False) == 5.0
        assert g.step_limit_pct(3, True) == 2.5

    def test_within_all_limits_passes_through(self):
        r = self.g.apply(1060, 1000, 500, 2000, 10)
        assert r.kcal == 1060 and r.changed and r.reason_codes == ()

    def test_step_limit_caps_increase(self):
        r = self.g.apply(1500, 1000, 500, 2000, 10)
        assert r.kcal == pytest.approx(1100) and r.reason_codes == ("RATE_LIMIT_APPLIED",)

    def test_step_limit_caps_decrease(self):
        r = self.g.apply(500, 1000, 400, 2000, 10)
        assert r.kcal == pytest.approx(900)

    def test_custom_step_reason_codes(self):
        r = self.g.apply(1500, 1000, 500, 2000, 5, step_reason_codes=("WARMUP_STEP_LIMIT",))
        assert r.reason_codes == ("WARMUP_STEP_LIMIT",)

    def test_floor_applied_flags(self):
        r = self.g.apply(600, 950, 900, 2000, 10)
        assert r.at_floor and not r.at_ceiling and "FLOOR_APPLIED" in r.reason_codes
        assert r.kcal == pytest.approx(900)

    def test_ceiling_applied_flags(self):
        r = self.g.apply(3000, 1900, 500, 2000, 10)
        assert r.at_ceiling and "CEILING_APPLIED" in r.reason_codes and r.kcal == pytest.approx(2000)

    def test_at_ceiling_but_change_below_threshold_keeps_plan(self):
        r = self.g.apply(3000, 1950, 500, 2000, 10)  # 2.6% < 4%
        assert r.at_ceiling and not r.changed and r.kcal == 1950

    def test_bounds_override_step_limit(self):
        # korak dozvoljava max 1100, ali pod je 1200 -> pod pobjeđuje
        r = self.g.apply(1500, 1000, 1200, 2000, 10)
        assert r.kcal == pytest.approx(1200)
        assert "PLAN_OUTSIDE_SAFE_BOUNDS" in r.reason_codes  # referenca (1000) je ispod poda

    def test_bounds_override_step_unreachable_when_reference_in_bounds(self):
        """Korak se uvijek kreće prema referenci; ako je referenca u granicama, ne može izaći iz njih.
        (Znači da je grana BOUNDS_OVERRIDE_STEP u GuardRails.apply mrtav kod.)"""
        import random
        rng = random.Random(7)
        for _ in range(1000):
            floor = rng.uniform(300, 900)
            ceiling = floor + rng.uniform(50, 2000)
            ref = rng.uniform(floor, ceiling)
            r = self.g.apply(rng.uniform(0, 5000), ref, floor, ceiling, rng.choice([2.5, 5, 10]))
            assert "BOUNDS_OVERRIDE_STEP" not in r.reason_codes

    def test_reference_above_ceiling_is_pulled_back_in(self):
        r = self.g.apply(2500, 3000, 500, 2000, 10)
        assert r.kcal == pytest.approx(2000)
        assert "PLAN_OUTSIDE_SAFE_BOUNDS" in r.reason_codes and r.at_ceiling

    def test_tiny_change_is_suppressed(self):
        r = self.g.apply(1020, 1000, 500, 2000, 10)  # 2% < 4%
        assert r.kcal == 1000 and not r.changed and "CHANGE_BELOW_THRESHOLD" in r.reason_codes

    def test_min_change_boundary(self):
        assert self.g.apply(1040, 1000, 500, 2000, 10).changed          # tačno 4% se prihvata
        assert not self.g.apply(1039.9, 1000, 500, 2000, 10).changed

    def test_min_change_can_be_disabled(self):
        r = self.g.apply(1010, 1000, 500, 2000, 10, enforce_min_change=False)
        assert r.kcal == 1010 and r.changed

    def test_min_change_not_enforced_when_reference_outside_bounds(self):
        # referenca (1000) je ispod poda (1005): prag minimalne promjene se ne primjenjuje
        r = self.g.apply(1010, 1000, 1005, 2000, 10)
        assert r.kcal == pytest.approx(1010) and r.changed and "CHANGE_BELOW_THRESHOLD" not in r.reason_codes

    def test_result_always_within_bounds_property(self):
        import random
        rng = random.Random(1)
        for _ in range(500):
            floor = rng.uniform(300, 900)
            ceiling = floor + rng.uniform(100, 2000)
            ref, proposed = rng.uniform(100, 3500), rng.uniform(0, 5000)
            r = self.g.apply(proposed, ref, floor, ceiling, rng.choice([2.5, 5, 10]))
            assert floor - 1e-9 <= r.kcal <= ceiling + 1e-9


def ctrl(state, target, config=CFG):
    cls = Classification(state, False, False, target, 0, 0)
    return Controller(SimpleNamespace(config=config, context={"classification": cls}))


class TestController:
    def test_ideal_has_zero_rate(self):
        assert ctrl(BodyState.IDEAL, 20).desired_rate_kg_per_week(20) == 0.0

    def test_overweight_loss_over_horizon(self):
        # cilj 24 kg, sad 28: gap -4 / 8 sedmica = -0.5 kg/sedm (limit 1% = 0.28 kg) -> -0.28
        assert ctrl(BodyState.OVERWEIGHT, 24).desired_rate_kg_per_week(28) == pytest.approx(-0.28)

    def test_overweight_small_gap_uses_horizon_rate(self):
        assert ctrl(BodyState.OVERWEIGHT, 19).desired_rate_kg_per_week(20) == pytest.approx(-0.125)

    def test_overweight_never_positive(self):
        assert ctrl(BodyState.OVERWEIGHT, 25).desired_rate_kg_per_week(20) == 0.0

    def test_underweight_gain_capped(self):
        assert ctrl(BodyState.UNDERWEIGHT, 20).desired_rate_kg_per_week(14) == pytest.approx(CFG.max_gain_pct_per_week / 100.0 * 14)

    def test_underweight_never_negative(self):
        assert ctrl(BodyState.UNDERWEIGHT, 10).desired_rate_kg_per_week(14) == 0.0

    def test_propose_kcal_adds_energy_deficit(self):
        kcal, rate = ctrl(BodyState.OVERWEIGHT, 24).propose_kcal(1200.0, 28)
        assert rate == pytest.approx(-0.28)
        assert kcal == pytest.approx(1200 + 7000 * -0.28 / 7)  # = 920

    def test_propose_kcal_maintenance(self):
        assert ctrl(BodyState.IDEAL, 20).propose_kcal(1200.0, 20) == (1200.0, 0.0)


class TestExplanations:
    def test_no_codes(self):
        assert Explanations.explain([], {}) == "Bez promjene."

    def test_formats_template_with_context(self):
        assert "1271 kcal" in Explanations.explain(["INITIAL_PLAN"], {"new_kcal": 1271.3})

    def test_unknown_code_falls_back_to_code(self):
        assert Explanations.explain(["SOMETHING_NEW"], {}) == "SOMETHING_NEW"

    def test_missing_context_renders_nan_not_crash(self):
        assert "nan" in Explanations.explain(["MODEL_ERROR_LARGE"], {})

    def test_bad_format_spec_falls_back_to_code(self):
        # string u kontekstu s numeričkim format specifikatorom -> ValueError -> kod
        assert Explanations.explain(["INITIAL_PLAN"], {"new_kcal": "abc"}) == "INITIAL_PLAN"

    def test_multiple_codes_joined(self):
        out = Explanations.explain(["IN_DEAD_ZONE", "CHANGE_BELOW_THRESHOLD"], {})
        assert out.count(".") >= 2 and "Težina je u zdravom rasponu" in out

    def test_every_template_renders_with_full_context(self):
        ctx = defaultdict_ctx()
        for code in Explanations._TEMPLATES:
            text = Explanations.explain([code], ctx)
            assert text and text != code, code


def defaultdict_ctx():
    return dict(new_kcal=1000.0, days_since_previous=3.0, detail="d", weight=20.0, prev_weight=18.0, eaten_kcal=900.0,
                bcs=9, eaten_ratio=0.5, z=4.0, mer_estimate=1200.0, prior_mer=1000.0, slope=-0.4, rate=-0.1,
                step_pct=5.0, floor=700.0, ceiling=2000.0)


class TestEmpiricalWeightSigma:
    calc = Calculations(CFG)
    T0 = datetime(2026, 1, 5, 8, 0, tzinfo=timezone.utc)

    def points(self, weights, gap_days=7):
        from datetime import timedelta
        return [(self.T0 + timedelta(days=gap_days * i), w) for i, w in enumerate(weights)]

    def test_too_few_points_returns_floor(self):
        assert self.calc.empirical_weight_sigma(self.points([20, 21, 19, 22, 20]), 0.15) == 0.15

    def test_never_below_floor(self):
        assert self.calc.empirical_weight_sigma(self.points([20.0] * 9), 0.15) == 0.15

    def test_linear_trend_without_noise_is_not_noise(self):
        weights = [20.0 - 0.2 * i for i in range(9)]
        assert self.calc.empirical_weight_sigma(self.points(weights), 0.15) == 0.15

    def test_recovers_large_noise_with_trend(self):
        import random
        rng = random.Random(3)
        weights = [20.0 - 0.2 * i + rng.gauss(0, 0.4) for i in range(9)]
        sigma = self.calc.empirical_weight_sigma(self.points(weights), 0.15)
        assert 0.25 < sigma < 0.7

    def test_capped_at_max_scale(self):
        weights = [20, 25, 15, 26, 14, 27, 13, 28, 12]
        assert self.calc.empirical_weight_sigma(self.points(weights), 0.15) == pytest.approx(
            CFG.noise_max_scale * 0.15)

    def test_skipped_week_does_not_look_like_noise(self):
        from datetime import timedelta
        times = [self.T0 + timedelta(days=d) for d in (0, 7, 14, 28, 35, 42)]
        weights = [20.0 - 0.2 * (d / 7) for d in (0, 7, 14, 28, 35, 42)]
        assert self.calc.empirical_weight_sigma(list(zip(times, weights)), 0.15) == 0.15
