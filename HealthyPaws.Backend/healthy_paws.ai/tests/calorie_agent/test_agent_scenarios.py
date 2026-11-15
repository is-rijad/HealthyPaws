"""Scenariji cijelog lanca CalorieAgent.run (validacija -> klasifikacija -> unos -> učenje -> sigurnost -> regulacija)."""
from datetime import timedelta

import pytest
from google.protobuf.json_format import MessageToDict

from calorie_agent.config.config import CalorieAgentConfig
from tests.conftest import (T0, action_name, make_intake, make_plan, make_request, run_agent, week)

HIST_STABLE = [week(w, 20.0) for w in (4, 3, 2, 1)]
GOOD_INTAKE = make_intake(eaten=350, offered=380)  # 350 g * 3.5 = 1225 kcal


def diag(response) -> dict:
    return MessageToDict(response.diagnostics)


def codes(response) -> list[str]:
    return list(response.reason_codes)


class TestFirstTick:
    def test_no_plan_creates_initial_plan_from_formula(self):
        r = run_agent(make_request())
        assert action_name(r) == "SET_INITIAL" and r.plan_changed and not r.has_error
        # RER(20)*1.6*1.2 = 1271.08, bez promjene jer je ideal + prvi korak unutar 5%
        assert r.new_daily_kcal == pytest.approx(1271.08, abs=0.01)
        assert r.new_daily_grams == pytest.approx(r.new_daily_kcal / 3.5)
        assert codes(r) == ["INITIAL_PLAN"] and not r.requires_human_review

    def test_intact_dog_gets_more_calories(self):
        neutered = run_agent(make_request(neutered=True)).new_daily_kcal
        intact = run_agent(make_request(neutered=False)).new_daily_kcal
        assert intact == pytest.approx(neutered * 1.8 / 1.6)

    def test_overweight_initial_plan_is_lower_but_within_first_step_limit(self):
        ideal = run_agent(make_request(weight=28, bcs=5, min_weight=18, max_weight=30)).new_daily_kcal
        over = run_agent(make_request(weight=28, bcs=9)).new_daily_kcal
        prior = run_agent(make_request(weight=28, bcs=9)).learned.mer_estimate_kcal
        assert over < prior and over >= prior * 0.95 - 1e-6 and ideal > 0

    def test_initial_plan_when_plan_exists_without_dose(self):
        r = run_agent(make_request(plan=make_plan(kcal=None, version=0)))
        assert action_name(r) == "SET_INITIAL"

    def test_response_metadata(self):
        r = run_agent(make_request(dog_id=42, correlation_id="abc"))
        assert r.dog_id == 42 and r.correlation_id == "abc"
        assert r.measured_at.ToDatetime() == T0.replace(tzinfo=None)
        assert r.next_evaluation_at.ToDatetime() == (T0 + timedelta(days=7)).replace(tzinfo=None)

    def test_first_tick_seeds_learned_state_with_prior(self):
        r = run_agent(make_request())
        assert r.learned.mer_estimate_kcal == pytest.approx(r.new_daily_kcal)
        assert r.learned.variance_kcal2 == pytest.approx((0.2 * r.learned.mer_estimate_kcal) ** 2)
        assert r.learned.n_updates == 0

    def test_plan_with_dose_but_no_history_is_first_measurement(self):
        r = run_agent(make_request(plan=make_plan(1250)))
        assert action_name(r) == "NO_CHANGE" and codes(r) == ["FIRST_MEASUREMENT"]
        assert r.new_daily_kcal == 1250 and not r.plan_changed


class TestGateways:
    def test_puppy_escalates(self):
        r = run_agent(make_request(life_stage="Puppy"))
        assert action_name(r) == "ESCALATE" and codes(r) == ["UNSUPPORTED_LIFE_STAGE"] and r.requires_human_review

    def test_pregnant_escalates(self):
        assert "UNSUPPORTED_LIFE_STAGE" in codes(run_agent(make_request(pregnant=True)))

    def test_measurement_too_soon(self):
        r = run_agent(make_request(history=[week(0.4, 20.0)], plan=make_plan(1250)))
        assert action_name(r) == "NO_CHANGE" and codes(r) == ["MEASUREMENT_TOO_SOON"]
        assert r.has_error
        assert "dana" in r.explanation

    def test_exactly_min_period_is_accepted(self):
        r = run_agent(make_request(history=[week(4 / 7, 20.0)], plan=make_plan(1250), intake=None))
        assert "MEASUREMENT_TOO_SOON" not in codes(r)

    def test_stale_measurement(self):
        r = run_agent(make_request(history=[week(-1, 20.0)], plan=make_plan(1250)))
        assert action_name(r) == "NO_CHANGE" and "STALE_MEASUREMENT" in codes(r)

    def test_invalid_weight_requests_remeasure(self):
        r = run_agent(make_request(weight=-1))
        assert action_name(r) == "REQUEST_REMEASURE" and codes(r) == ["INVALID_MEASUREMENT"]
        assert not r.requires_human_review and "težina" in r.explanation

    def test_invalid_bcs_requests_remeasure(self):
        assert action_name(run_agent(make_request(bcs=4))) == "REQUEST_REMEASURE"

    def test_invalid_config_escalates(self):
        r = run_agent(make_request(food_kcal=100))
        assert action_name(r) == "ESCALATE" and codes(r) == ["INVALID_INPUT"] and r.requires_human_review

    def test_validation_diagnostics_recorded(self):
        r = run_agent(make_request(weight=-1))
        assert diag(r)["validation"][0]["field"] == "measurement.weight"

    def test_warnings_do_not_block(self):
        r = run_agent(make_request(history=[week(1, 20.0, bcs=4)], plan=make_plan(1250)))
        assert action_name(r) != "REQUEST_REMEASURE"
        assert any(v["severity"] == "Warn" for v in diag(r)["validation"])

    def test_invalid_history_weights_are_ignored(self):
        r = run_agent(make_request(history=[week(1, -5.0)], plan=make_plan(1250)))
        assert codes(r) == ["FIRST_MEASUREMENT"]  # jedini red je nevaljan -> nema prethodnog mjerenja

    def test_history_order_does_not_matter(self):
        a = run_agent(make_request(history=HIST_STABLE, plan=make_plan(1250), intake=GOOD_INTAKE))
        b = run_agent(make_request(history=list(reversed(HIST_STABLE)), plan=make_plan(1250), intake=GOOD_INTAKE))
        assert (a.action, list(a.reason_codes), a.new_daily_kcal) == (b.action, list(b.reason_codes), b.new_daily_kcal)

    def test_contradictory_bcs_and_weight_escalates(self):
        r = run_agent(make_request(weight=15, bcs=9, min_weight=18, max_weight=24, plan=make_plan(1250)))
        assert "CONTRADICTORY_BCS_WEIGHT" in codes(r) and action_name(r) == "ESCALATE"

    def test_correlation_id_and_plan_version_echoed_on_every_path(self):
        r = run_agent(make_request(plan=make_plan(1250, version=7), correlation_id="zzz", history=[week(0.1, 20)]))
        assert r.correlation_id == "zzz" and r.plan_version == 7


class TestRemeasureConfirmation:
    """Dvostepena potvrda: sumnjiva težina -> REQUEST_REMEASURE (pamti pending) -> druga slična težina -> ESCALATE."""

    def test_implausible_jump_requests_remeasure_and_stores_pending(self):
        r = run_agent(make_request(weight=26, history=HIST_STABLE, plan=make_plan(1250)))
        assert action_name(r) == "REQUEST_REMEASURE" and codes(r) == ["REMEASURE_IMPLAUSIBLE_CHANGE"]
        assert r.learned.pending_weight_kg == 26 and not r.requires_human_review
        assert "20.0" in r.explanation and "26.0" in r.explanation

    def test_matching_second_weight_escalates_and_clears_pending(self):
        r = run_agent(make_request(weight=26.2, history=HIST_STABLE, plan=make_plan(1250, pending_weight_kg=26.0)))
        assert action_name(r) == "ESCALATE" and codes(r) == ["RAPID_CHANGE_CONFIRMED"]
        assert not r.learned.HasField("pending_weight_kg") and r.requires_human_review

    def test_non_matching_second_weight_replaces_pending(self):
        r = run_agent(make_request(weight=28, history=HIST_STABLE, plan=make_plan(1250, pending_weight_kg=26.0)))
        assert action_name(r) == "REQUEST_REMEASURE" and r.learned.pending_weight_kg == 28

    def test_plausible_weight_clears_stale_pending(self):
        r = run_agent(make_request(weight=20.1, history=HIST_STABLE, plan=make_plan(1250, pending_weight_kg=26.0),
                                   intake=GOOD_INTAKE))
        assert not r.learned.HasField("pending_weight_kg")

    def test_out_of_range_weight_needs_confirmation_then_escalates(self):
        first = run_agent(make_request(weight=60, history=HIST_STABLE, plan=make_plan(1250)))
        assert codes(first) == ["INVALID_MEASUREMENT"] and first.learned.pending_weight_kg == 60
        second = run_agent(make_request(weight=60.5, history=HIST_STABLE,
                                        plan=make_plan(1250, pending_weight_kg=first.learned.pending_weight_kg)))
        assert codes(second) == ["WEIGHT_OUT_OF_RANGE_CONFIRMED"] and action_name(second) == "ESCALATE"
        assert not second.learned.HasField("pending_weight_kg")

    def test_confirmation_tolerance_boundary(self):
        # tolerancija = max(3%, 3*sigma/w): 26 -> 3% = 0.78
        near = run_agent(make_request(weight=26.7, history=HIST_STABLE, plan=make_plan(1250, pending_weight_kg=26.0)))
        far = run_agent(make_request(weight=27.0, history=HIST_STABLE, plan=make_plan(1250, pending_weight_kg=26.0)))
        assert "RAPID_CHANGE_CONFIRMED" in codes(near) and "RAPID_CHANGE_CONFIRMED" not in codes(far)

    def test_small_change_within_noise_floor_is_accepted_even_for_short_history(self):
        r = run_agent(make_request(weight=20.3, history=[week(1, 20.0)], plan=make_plan(1250), intake=GOOD_INTAKE))
        assert "REMEASURE_IMPLAUSIBLE_CHANGE" not in codes(r)


class TestIntakeChecks:
    base = dict(history=HIST_STABLE, plan=make_plan(1250))

    def test_no_intake_block(self):
        r = run_agent(make_request(**self.base, intake=None))
        assert action_name(r) == "NO_CHANGE" and codes(r) == ["NO_INTAKE_DATA"]

    def test_unset_eaten_means_no_data(self):
        assert codes(run_agent(make_request(**self.base, intake=make_intake(eaten=None)))) == ["NO_INTAKE_DATA"]

    def test_low_coverage_means_no_data(self):
        r = run_agent(make_request(**self.base, intake=make_intake(days_covered=4.0, period_days=7)))
        assert codes(r) == ["NO_INTAKE_DATA"]

    def test_coverage_boundary(self):
        ok = run_agent(make_request(**self.base, intake=make_intake(days_covered=4.9, period_days=7)))
        assert "NO_INTAKE_DATA" not in codes(ok)

    def test_zero_period_is_validation_ok_but_no_data(self):
        r = run_agent(make_request(**self.base, intake=make_intake(days_covered=0, period_days=0)))
        assert codes(r) == ["NO_INTAKE_DATA"]

    def test_reduced_appetite_escalates(self):
        r = run_agent(make_request(**self.base, intake=make_intake(eaten=250, offered=380)))
        assert action_name(r) == "ESCALATE" and codes(r) == ["REDUCED_APPETITE"] and r.requires_human_review
        assert "66%" in r.explanation

    def test_appetite_boundary(self):
        ok = run_agent(make_request(**self.base, intake=make_intake(eaten=266, offered=380)))  # 70%
        assert "REDUCED_APPETITE" not in codes(ok)

    def test_implausibly_low_kcal_escalates(self):
        # 20 g * 3.5 = 70 kcal < 0.2 * RER(20) (=132)
        r = run_agent(make_request(**self.base, intake=make_intake(eaten=20, offered=21)))
        assert action_name(r) == "ESCALATE" and codes(r) == ["INTAKE_IMPLAUSIBLE"]

    def test_sensor_fault_high_intake_is_no_change(self):
        r = run_agent(make_request(**self.base, intake=make_intake(eaten=1000, offered=1000)))
        assert action_name(r) == "NO_CHANGE" and codes(r) == ["INTAKE_IMPLAUSIBLE"] and not r.requires_human_review

    def test_treats_are_added_to_energy_balance_but_not_checks(self):
        without = run_agent(make_request(**self.base, intake=make_intake(treat_kcal=None)))
        withtreat = run_agent(make_request(**self.base, intake=make_intake(treat_kcal=100)))
        assert diag(withtreat)["treat_kcal"] == 100 and diag(withtreat)["feeder_kcal"] == diag(without)["feeder_kcal"]
        assert diag(withtreat)["observed_mer"] == pytest.approx(diag(without)["observed_mer"] + 100)

    @pytest.mark.parametrize("treat", [0.0, -50.0])
    def test_non_positive_treats_ignored(self, treat):
        r = run_agent(make_request(**self.base, intake=make_intake(treat_kcal=treat)))
        assert diag(r)["treat_kcal"] == 0.0


class TestLearning:
    def test_stable_weight_with_matching_intake_updates_estimate(self):
        r = run_agent(make_request(history=HIST_STABLE, plan=make_plan(1250), intake=GOOD_INTAKE))
        assert action_name(r) == "NO_CHANGE" and codes(r) == ["IN_DEAD_ZONE"]
        assert r.learned.n_updates == 1 and r.learned.consecutive_bad_weeks == 0
        d = diag(r)
        assert d["observed_mer"] == pytest.approx(1225.0)  # težina stabilna -> potrošnja = unos
        prior = 70 * 20 ** 0.75 * 1.6 * 1.2
        assert min(prior, 1225.0) < r.learned.mer_estimate_kcal < max(prior, 1225.0)
        assert r.learned.variance_kcal2 < (0.2 * prior) ** 2

    def test_learned_state_is_carried_forward(self):
        r = run_agent(make_request(history=HIST_STABLE, intake=GOOD_INTAKE,
                                   plan=make_plan(1250, mer_estimate_kcal=1225.0, variance_kcal2=2500.0, n_updates=5)))
        assert r.learned.n_updates == 6 and r.learned.mer_estimate_kcal == pytest.approx(1225.0, abs=1)
        assert r.learned.variance_kcal2 < 2500.0 + 20 ** 2

    def test_implausible_energy_balance_is_bad_week_not_learned(self):
        # unos 1225 kcal, ali pas "izgubio" 1 kg u 7 dana -> potrošnja 2225 -> |z| velik
        hist = [week(w, 21.0 if w == 1 else 20.5 + 0.0) for w in (4, 3, 2, 1)]
        r = run_agent(make_request(weight=20.0, history=hist, plan=make_plan(1250,
                                   mer_estimate_kcal=1225.0, variance_kcal2=2500.0), intake=GOOD_INTAKE))
        assert action_name(r) == "NO_CHANGE" and codes(r) == ["MODEL_ERROR_LARGE"]
        assert r.learned.consecutive_bad_weeks == 1 and r.learned.n_updates == 0
        assert r.learned.mer_estimate_kcal == pytest.approx(1225.0)  # procjena se ne dira
        assert abs(diag(r)["innovation_z"]) > 3

    def test_persistent_bad_weeks_escalate(self):
        hist = [week(w, 21.0 if w == 1 else 20.5) for w in (4, 3, 2, 1)]
        r = run_agent(make_request(weight=20.0, history=hist, intake=GOOD_INTAKE,
                                   plan=make_plan(1250, mer_estimate_kcal=1225.0, variance_kcal2=2500.0,
                                                  consecutive_bad_weeks=2)))
        assert action_name(r) == "ESCALATE" and codes(r) == ["MODEL_MISMATCH_PERSISTENT"]
        assert r.learned.consecutive_bad_weeks == 3 and r.requires_human_review

    def test_good_week_resets_bad_week_counter(self):
        r = run_agent(make_request(history=HIST_STABLE, intake=GOOD_INTAKE,
                                   plan=make_plan(1250, mer_estimate_kcal=1225.0, variance_kcal2=2500.0,
                                                  consecutive_bad_weeks=2)))
        assert r.learned.consecutive_bad_weeks == 0

    def test_corrupt_learned_state_is_reset_with_reason(self):
        r = run_agent(make_request(history=HIST_STABLE, intake=GOOD_INTAKE,
                                   plan=make_plan(1250, mer_estimate_kcal=-5.0, variance_kcal2=100.0)))
        assert "LEARNED_STATE_RESET" in codes(r) and r.learned.mer_estimate_kcal > 0

    def test_estimate_far_from_prior_escalates(self):
        # procjena 1843 je 45% iznad formule (1271); mala varijanca + unos 1750 kcal = nema "loše sedmice", a procjena ostaje daleko
        r = run_agent(make_request(history=HIST_STABLE, intake=make_intake(eaten=500, offered=520),
                                   plan=make_plan(1250, mer_estimate_kcal=1843.0, variance_kcal2=100.0)))
        assert "ESTIMATE_FAR_FROM_PRIOR" in codes(r) and action_name(r) == "ESCALATE"
        assert r.requires_human_review

    def test_large_variance_estimate_follows_observation_instead_of_escalating(self):
        r = run_agent(make_request(history=HIST_STABLE, intake=GOOD_INTAKE,
                                   plan=make_plan(1250, mer_estimate_kcal=2500.0, variance_kcal2=1e9)))
        assert "ESTIMATE_FAR_FROM_PRIOR" not in codes(r)
        assert r.learned.mer_estimate_kcal == pytest.approx(1225.0, abs=5)


class TestSafety:
    def test_steady_decline_when_not_on_diet_escalates(self):
        hist = [week(4, 20.0), week(3, 19.6), week(2, 19.2), week(1, 18.8)]
        r = run_agent(make_request(weight=18.4, history=hist, plan=make_plan(1250), intake=make_intake(eaten=360, offered=380)))
        assert action_name(r) == "ESCALATE" and codes(r)[0] in ("UNEXPLAINED_WEIGHT_LOSS",)
        assert r.requires_human_review and "pregled kod veterinara" in r.explanation

    def test_same_decline_for_overweight_dog_on_diet_is_not_alarming(self):
        hist = [week(4, 30.0, 9), week(3, 29.7, 9), week(2, 29.4, 9), week(1, 29.1, 9)]
        r = run_agent(make_request(weight=28.8, bcs=9, history=hist, plan=make_plan(1250),
                                   intake=make_intake(eaten=300, offered=330)))
        assert "UNEXPLAINED_WEIGHT_LOSS" not in codes(r) and "RAPID_LOSS_ON_DIET" not in codes(r)

    def test_too_fast_loss_on_diet_escalates_with_rapid_code(self):
        hist = [week(4, 33.0, 9), week(3, 32.0, 9), week(2, 31.0, 9), week(1, 30.0, 9)]
        r = run_agent(make_request(weight=29.0, bcs=9, history=hist, min_weight=18, max_weight=24,
                                   plan=make_plan(1250), intake=make_intake(eaten=300, offered=330)))
        assert action_name(r) == "ESCALATE" and "RAPID_LOSS_ON_DIET" in codes(r)

    def test_noise_level_fluctuation_is_not_alarming(self):
        hist = [week(4, 20.0), week(3, 20.15), week(2, 19.9), week(1, 20.1)]
        r = run_agent(make_request(weight=19.95, history=hist, plan=make_plan(1250), intake=GOOD_INTAKE))
        assert action_name(r) == "NO_CHANGE" and "UNEXPLAINED_WEIGHT_LOSS" not in codes(r)

    def test_trend_diagnostics_present_with_enough_history(self):
        r = run_agent(make_request(history=HIST_STABLE, plan=make_plan(1250), intake=GOOD_INTAKE))
        d = diag(r)
        assert d["slope_kg_per_week"] == pytest.approx(0.0, abs=1e-9) and d["slope_se"] > 0

    def test_window_diagnostics_need_minimum_rows(self):
        few = run_agent(make_request(history=HIST_STABLE[:2], plan=make_plan(1250), intake=GOOD_INTAKE))
        enough = run_agent(make_request(history=HIST_STABLE, plan=make_plan(1250), intake=GOOD_INTAKE))
        assert "window_loss_kg" not in diag(few) and "window_loss_kg" in diag(enough)


class TestRegulation:
    def test_overweight_dog_gets_decrease_within_step_limit(self):
        hist = [week(w, 28.0, 9) for w in (4, 3, 2, 1)]
        r = run_agent(make_request(weight=28.0, bcs=9, history=hist, plan=make_plan(1250), intake=make_intake(eaten=330, offered=380)))
        assert action_name(r) == "DECREASE" and r.plan_changed
        assert 1250 * 0.95 - 1e-6 <= r.new_daily_kcal < 1250  # warmup: max 5%
        assert "WEIGHT_LOSS_TARGET" in codes(r) and "WARMUP_STEP_LIMIT" in codes(r)
        assert r.new_daily_grams == pytest.approx(r.new_daily_kcal / 3.5)

    def test_underweight_dog_gets_increase(self):
        hist = [week(w, 15.0, 1) for w in (4, 3, 2, 1)]
        r = run_agent(make_request(weight=15.0, bcs=1, history=hist, plan=make_plan(1100), intake=make_intake(eaten=330, offered=380)))
        assert action_name(r) == "INCREASE" and r.new_daily_kcal > 1100 and "WEIGHT_GAIN_TARGET" in codes(r)

    def test_after_warmup_larger_steps_allowed(self):
        hist = [week(w, 28.0, 9) for w in (4, 3, 2, 1)]
        kw = dict(weight=28.0, bcs=9, history=hist, intake=make_intake(eaten=330, offered=380))
        warm = run_agent(make_request(**kw, plan=make_plan(1800, mer_estimate_kcal=1300.0, variance_kcal2=2500.0, n_updates=0)))
        after = run_agent(make_request(**kw, plan=make_plan(1800, mer_estimate_kcal=1300.0, variance_kcal2=2500.0, n_updates=3)))
        assert after.new_daily_kcal < warm.new_daily_kcal  # veći korak nakon zagrijavanja

    def test_step_never_exceeds_limit_after_warmup(self):
        hist = [week(w, 28.0, 9) for w in (4, 3, 2, 1)]
        r = run_agent(make_request(weight=28.0, bcs=9, history=hist, intake=make_intake(eaten=330, offered=380),
                                   plan=make_plan(1800, mer_estimate_kcal=1300.0, variance_kcal2=2500.0, n_updates=5)))
        assert r.new_daily_kcal >= 1800 * 0.9 - 1e-6

    def test_conflict_halves_step_limit(self):
        hist = [week(w, 22.0, 9) for w in (4, 3, 2, 1)]  # BCS 9 potvrđen, težina u rasponu -> konflikt
        r = run_agent(make_request(weight=22.0, bcs=9, history=hist, intake=make_intake(eaten=330, offered=380),
                                   plan=make_plan(1800, mer_estimate_kcal=1300.0, variance_kcal2=2500.0, n_updates=5)))
        assert "CONFLICT_STEP_LIMIT" in codes(r) and r.new_daily_kcal >= 1800 * 0.95 - 1e-6

    def test_ideal_and_stable_stays_in_dead_zone(self):
        r = run_agent(make_request(history=HIST_STABLE, plan=make_plan(1250), intake=GOOD_INTAKE))
        assert codes(r) == ["IN_DEAD_ZONE"] and r.new_daily_kcal == 1250 and not r.plan_changed

    def test_arrival_at_target_triggers_regulation_to_maintenance(self):
        # zdrav tempo ~0.2 kg/sedmično; zadnji BCS 7 -> sada 5 = "upravo stigao na cilj"
        hist = [week(4, 22.0, 7), week(3, 21.8, 7), week(2, 21.6, 7), week(1, 21.4, 7)]
        r = run_agent(make_request(weight=21.2, bcs=5, history=hist, intake=make_intake(eaten=300, offered=330),
                                   plan=make_plan(1000, mer_estimate_kcal=1100.0, variance_kcal2=2500.0, n_updates=5)))
        assert "TARGET_REACHED" in codes(r) and diag(r)["arrived_at_target"] is True
        assert "IN_DEAD_ZONE" not in codes(r) and "MAINTENANCE_TARGET" in codes(r)

    def test_small_adjustment_below_threshold_is_not_applied(self):
        r = run_agent(make_request(history=[week(w, 20.0, 5) for w in (4, 3, 2, 1)], intake=make_intake(eaten=350, offered=380),
                                   plan=make_plan(1225, mer_estimate_kcal=1225.0, variance_kcal2=100.0, n_updates=5)))
        assert not r.plan_changed and r.new_daily_kcal == 1225

    def _stuck_request(self, weeks_at_bound):
        # prior-provjera se isključuje konfiguracijom da bismo izolovali logiku "zaglavljen na granici"
        hist = [week(w, 30.0, 9) for w in (4, 3, 2, 1)]
        request = make_request(weight=30.0, bcs=9, history=hist, intake=make_intake(eaten=228, offered=240),
                               plan=make_plan(690, mer_estimate_kcal=800.0, variance_kcal2=400.0, n_updates=5,
                                              weeks_at_bound=weeks_at_bound))
        return request, CalorieAgentConfig(prior_deviation_max=10.0)

    def test_plan_stuck_at_floor_counts_weeks(self):
        request, cfg = self._stuck_request(0)
        r = run_agent(request, cfg)
        assert r.learned.weeks_at_bound == 1 and "FLOOR_APPLIED" in codes(r)
        assert action_name(r) in ("NO_CHANGE", "DECREASE")

    def test_plan_stuck_at_floor_escalates_after_three_weeks(self):
        request, cfg = self._stuck_request(2)
        r = run_agent(request, cfg)
        assert action_name(r) == "ESCALATE" and codes(r) == ["PLAN_AT_BOUND_NO_PROGRESS"]
        assert r.learned.weeks_at_bound == 3 and r.requires_human_review

    def test_dead_zone_resets_weeks_at_bound(self):
        r = run_agent(make_request(history=HIST_STABLE, plan=make_plan(1250, weeks_at_bound=2), intake=GOOD_INTAKE))
        assert r.learned.weeks_at_bound == 0


class TestConfigurability:
    def test_custom_config_changes_behaviour(self):
        cfg = CalorieAgentConfig(mer_factor_neutered=2.0)
        default = run_agent(make_request())
        custom = run_agent(make_request(), cfg)
        assert custom.learned.mer_estimate_kcal == pytest.approx(default.learned.mer_estimate_kcal * 2.0 / 1.6)

    def test_agent_instances_do_not_share_state(self):
        a = run_agent(make_request(weight=-1))
        b = run_agent(make_request())
        assert codes(b) == ["INITIAL_PLAN"] and codes(a) == ["INVALID_MEASUREMENT"]

    def test_has_error_is_only_set_for_invalid_input(self):
        """Backend (CalorieAgent.cs) baca BadRequest i NE sprema odgovor kad je has_error=true.
        Zato smije biti true samo za nevaljan ulaz (uključujući prerano mjerenje, koje se odbija); sve ostale odluke (i NoChange/Escalate/RequestRemeasure
        zbog sumnjive težine) moraju biti zabilježene."""
        stable = run_agent(make_request(history=HIST_STABLE, plan=make_plan(1250), intake=GOOD_INTAKE))
        initial = run_agent(make_request())
        regulated = run_agent(make_request(weight=28.0, bcs=9, history=[week(w, 28.0, 9) for w in (4, 3, 2, 1)],
                                           plan=make_plan(1250), intake=make_intake(eaten=330, offered=380)))
        assert not (stable.has_error or initial.has_error or regulated.has_error)

        benign_stops = {
            "dead zone": make_request(history=HIST_STABLE, plan=make_plan(1250), intake=GOOD_INTAKE),
            "no intake": make_request(history=HIST_STABLE, plan=make_plan(1250), intake=None),
            "puppy": make_request(life_stage="puppy"),
            "reduced appetite": make_request(history=HIST_STABLE, plan=make_plan(1250), intake=make_intake(eaten=200, offered=380)),
            "implausible jump (pending weight)": make_request(weight=26, history=HIST_STABLE, plan=make_plan(1250)),
            "out of range (pending weight)": make_request(weight=60, history=HIST_STABLE, plan=make_plan(1250)),
            "first measurement": make_request(plan=make_plan(1250)),
        }
        for name, request in benign_stops.items():
            assert not run_agent(request).has_error, name

        for name, request in {
            "negative weight": make_request(weight=-1),
            "bad bcs": make_request(bcs=4),
            "bad food kcal": make_request(food_kcal=100),
            "too soon": make_request(history=[week(0.3, 20)], plan=make_plan(1250)),
        }.items():
            assert run_agent(request).has_error, name
