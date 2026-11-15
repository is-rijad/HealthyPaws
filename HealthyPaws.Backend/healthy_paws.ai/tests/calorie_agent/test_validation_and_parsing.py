from datetime import timedelta, timezone

import pytest

from calorie_agent.calorie_agent import CalorieAgent
from calorie_agent.config.config import CalorieAgentConfig
from calorie_agent.core.classification.body_state import BodyState
from calorie_agent.interfaces.learned_state import LearnedState
from calorie_agent.interfaces.tick_request import TickRequest
from calorie_agent.validators.severity import Severity
from calorie_agent.validators.tick_request_validator import TickRequestValidator as V
from tests.conftest import T0, make_intake, make_plan, make_request, week

CFG = CalorieAgentConfig()


def issues(**kw):
    return V.validate(TickRequest.serialize(make_request(**kw)), CFG)


def fields(found):
    return {(i.severity, i.field) for i in found}


class TestSerialize:
    def test_maps_all_fields(self):
        req = make_request(history=[week(1, 19.5, 5)], plan=make_plan(1200.0, 3, mer_estimate_kcal=1300.0,
                                                                         variance_kcal2=500.0, n_updates=4),
                           intake=make_intake(treat_kcal=50))
        t = TickRequest.serialize(req)
        assert t.correlation_id == "cid-1" and t.measurement.dog_id == 1 and t.measurement.weight == 20.0
        assert t.measurement.measured_at == T0 and t.measurement.measured_at.tzinfo is timezone.utc
        assert len(t.history) == 1 and t.history[0].weight_kg == 19.5 and t.history[0].bcs == 5
        assert t.intake.treat_kcal_per_day == 50 and t.intake.days_covered == 7
        assert t.profile.neutered and t.profile.activity_factor == "1.2"
        assert t.plan.current_daily_kcal == 1200.0 and t.plan.plan_version == 3
        assert t.plan.learned.mer_estimate_kcal == 1300.0 and t.plan.learned.n_updates == 4

    def test_unset_optionals_become_none(self):
        t = TickRequest.serialize(make_request(history=[week(1, 19.5, bcs=None)],
                                               intake=make_intake(eaten=None, offered=None),
                                               plan=make_plan(kcal=None)))
        assert t.history[0].bcs is None and t.history[0].eaten_g_per_day is None
        assert t.intake.eaten_g_per_day is None and t.intake.treat_kcal_per_day is None
        assert t.plan.current_daily_kcal is None and t.plan.learned.mer_estimate_kcal is None
        assert t.plan.learned.last_update_at is None and t.plan.learned.pending_weight_kg is None

    def test_zero_is_distinguished_from_unset(self):
        t = TickRequest.serialize(make_request(intake=make_intake(eaten=0.0), history=[week(1, 19.5, bcs=0)]))
        assert t.intake.eaten_g_per_day == 0.0 and t.history[0].bcs == 0

    def test_missing_intake_and_plan(self):
        t = TickRequest.serialize(make_request(intake=None))
        assert t.intake is None and t.plan is None

    def test_last_update_at_roundtrip(self):
        t = TickRequest.serialize(make_request(plan=make_plan(last_update_at=T0 - timedelta(days=7))))
        assert t.plan.learned.last_update_at == T0 - timedelta(days=7)


class TestMeasurementValidation:
    def test_valid_request_has_no_issues(self):
        assert issues() == ()

    @pytest.mark.parametrize("weight", [0.0, -3.0])
    def test_non_positive_weight(self, weight):
        found = issues(weight=weight)
        assert (Severity.REMEASURE, "measurement.weight") in fields(found)
        assert not found[0].confirmable

    @pytest.mark.parametrize("weight", [float("nan"), float("inf"), float("-inf")])
    def test_non_finite_weight(self, weight):
        assert (Severity.REMEASURE, "measurement") in fields(issues(weight=weight))

    @pytest.mark.parametrize("weight", [0.4, 121.0])
    def test_weight_outside_plausible_range_is_confirmable(self, weight):
        found = [i for i in issues(weight=weight, min_weight=0.3, max_weight=130) if i.field == "measurement.weight"]
        assert found and found[0].confirmable

    def test_plausible_range_boundaries_inclusive(self):
        assert issues(weight=0.5, min_weight=0.4, max_weight=0.6) == ()
        assert issues(weight=120.0, min_weight=100, max_weight=120.0) == ()

    def test_weight_far_from_breed_range_is_confirmable(self):
        found = issues(weight=60.0, min_weight=18, max_weight=24)  # > 24*2
        assert found[0].confirmable and found[0].field == "measurement.weight"

    def test_weight_within_ratio_of_range_is_ok(self):
        assert issues(weight=47.0, min_weight=18, max_weight=24) == ()   # < 48
        assert issues(weight=9.5, min_weight=18, max_weight=24) == ()    # > 9

    @pytest.mark.parametrize("bcs", [0, 2, 4, 6, 8, 10, -1])
    def test_even_or_out_of_range_bcs_rejected(self, bcs):
        assert (Severity.REMEASURE, "measurement.bcs") in fields(issues(bcs=bcs))

    @pytest.mark.parametrize("bcs", [1, 3, 5, 7, 9])
    def test_valid_bcs_accepted(self, bcs):
        assert issues(bcs=bcs) == ()


class TestConfigurationValidation:
    @pytest.mark.parametrize("lo,hi", [(0, 24), (-1, 24), (24, 18), (20, 20)])
    def test_bad_weight_range(self, lo, hi):
        assert (Severity.ESCALATE, "measurement.minWeight/maxWeight") in fields(issues(min_weight=lo, max_weight=hi))

    def test_range_too_wide(self):
        assert (Severity.ESCALATE, "measurement.minWeight/maxWeight") in fields(issues(min_weight=10, max_weight=26))

    def test_range_exactly_max_ratio_is_ok(self):
        assert not [i for i in issues(weight=15, min_weight=10, max_weight=25) if i.severity is Severity.ESCALATE]

    @pytest.mark.parametrize("kcal", [0.4, 6.1, float("nan"), float("inf")])
    def test_food_kcal_out_of_range(self, kcal):
        assert (Severity.ESCALATE, "profile.foodKcalPerGram") in fields(issues(food_kcal=kcal))

    @pytest.mark.parametrize("kcal", [0.5, 6.0])
    def test_food_kcal_boundaries_ok(self, kcal):
        assert issues(food_kcal=kcal) == ()

    @pytest.mark.parametrize("activity", ["abc", "", "1.5", "nan"])
    def test_unsupported_activity_factor(self, activity):
        assert (Severity.ESCALATE, "profile.activityFactor") in fields(issues(activity=activity))

    @pytest.mark.parametrize("kcal", [0.0, -5.0])
    def test_non_positive_plan(self, kcal):
        assert (Severity.ESCALATE, "plan.currentDailyKcal") in fields(issues(plan=make_plan(kcal)))

    def test_plan_without_dose_is_not_an_error(self):
        assert issues(plan=make_plan(kcal=None)) == ()

    def test_negative_intake(self):
        assert (Severity.ESCALATE, "intake.eatenGramsPerDay") in fields(issues(intake=make_intake(eaten=-1)))
        assert (Severity.ESCALATE, "intake.offeredGramsPerDay") in fields(issues(intake=make_intake(offered=-1)))

    @pytest.mark.parametrize("covered,period", [(8, 7), (-1, 7), (1, -1)])
    def test_bad_intake_period(self, covered, period):
        assert (Severity.ESCALATE, "intake.daysCovered/periodDays") in fields(
            issues(intake=make_intake(days_covered=covered, period_days=period)))

    def test_days_covered_slack_allowed(self):
        assert issues(intake=make_intake(days_covered=7.5, period_days=7)) == ()

    def test_eaten_much_more_than_offered_is_only_a_warning(self):
        found = issues(intake=make_intake(eaten=500, offered=380))
        assert fields(found) == {(Severity.WARN, "intake.eatenGramsPerDay")}
        assert V.first_blocking(found) is None


class TestHistoryValidation:
    def test_invalid_history_weight_is_warning(self):
        found = issues(history=[week(1, -2.0)])
        assert (Severity.WARN, "history[0].weightKg") in fields(found)

    def test_history_not_older_than_current(self):
        assert (Severity.WARN, "history[0].measuredAt") in fields(issues(history=[week(0, 19.5)]))
        assert (Severity.WARN, "history[0].measuredAt") in fields(issues(history=[week(-1, 19.5)]))

    def test_duplicate_timestamps(self):
        found = issues(history=[week(1, 19.5), week(1, 19.6)])
        assert (Severity.WARN, "history[1].measuredAt") in fields(found)

    def test_invalid_history_bcs(self):
        assert (Severity.WARN, "history[0].bcs") in fields(issues(history=[week(1, 19.5, bcs=4)]))

    def test_history_issues_never_block(self):
        found = issues(history=[week(0, -1, bcs=4)])
        assert found and V.first_blocking(found) is None


class TestIntakeProblem:
    def test_ratio_thresholds(self):
        assert V.intake_problem(1000, 400, CFG) is None
        assert V.intake_problem(2100, 400, CFG) is Severity.WARN        # > 5x RER
        assert V.intake_problem(70, 400, CFG) is Severity.ESCALATE      # < 0.2x RER

    def test_boundaries_are_ok(self):
        assert V.intake_problem(2000, 400, CFG) is None
        assert V.intake_problem(80, 400, CFG) is None

    def test_non_positive_rer(self):
        assert V.intake_problem(100, 0, CFG) is None


class TestFirstBlocking:
    def test_returns_first_non_warning(self):
        from calorie_agent.validators.validation_issue import ValidationIssue as VI
        a, b, c = VI(Severity.WARN, "a", ""), VI(Severity.ESCALATE, "b", ""), VI(Severity.REMEASURE, "c", "")
        assert V.first_blocking((a, b, c)) is b
        assert V.first_blocking((a,)) is None and V.first_blocking(()) is None

    def test_to_dict(self):
        from calorie_agent.validators.validation_issue import ValidationIssue as VI
        assert VI(Severity.WARN, "f", "d", True).to_dict() == {"severity": "Warn", "field": "f", "detail": "d",
                                                              "confirmable": True}


def sane(learned: LearnedState | None):
    agent = CalorieAgent.__new__(CalorieAgent)
    agent.reason_codes = []
    agent.plan = None if learned is None else type("P", (), {"learned": learned})()
    return LearnedState.sane_learned(agent), agent.reason_codes


class TestSaneLearned:
    def test_no_plan_gives_empty_state(self):
        s, codes = sane(None)
        assert s == LearnedState() and codes == []

    def test_valid_state_returned_untouched(self):
        original = LearnedState(1200.0, 400.0, 3, 1, 0, 19.0)
        s, codes = sane(original)
        assert s is original and codes == []

    def test_both_unset_is_valid(self):
        assert sane(LearnedState())[1] == []

    @pytest.mark.parametrize("mer,var", [(1200.0, None), (None, 400.0), (-1.0, 400.0), (1200.0, 0.0),
                                         (float("nan"), 400.0), (1200.0, float("inf")), (0.0, 400.0)])
    def test_inconsistent_estimate_is_reset_together(self, mer, var):
        s, codes = sane(LearnedState(mer, var, 5))
        assert s.mer_estimate_kcal is None and s.variance_kcal2 is None
        assert s.n_updates == 5 and codes == ["LEARNED_STATE_RESET"]

    @pytest.mark.parametrize("pending", [0.0, -2.0, float("nan"), float("inf")])
    def test_bad_pending_weight_cleared(self, pending):
        s, codes = sane(LearnedState(pending_weight_kg=pending))
        assert s.pending_weight_kg is None and codes == ["LEARNED_STATE_RESET"]

    def test_negative_counters_clamped(self):
        s, codes = sane(LearnedState(n_updates=-1, consecutive_bad_weeks=-5, weeks_at_bound=-2))
        assert (s.n_updates, s.consecutive_bad_weeks, s.weeks_at_bound) == (0, 0, 0)
        assert codes == ["LEARNED_STATE_RESET"]

    def test_to_pb2_roundtrip(self):
        pb = LearnedState(1200.0, 400.0, 3, 1, 2, None, T0).to_pb2()
        assert pb.mer_estimate_kcal == 1200.0 and pb.n_updates == 3 and pb.weeks_at_bound == 2
        assert not pb.HasField("pending_weight_kg") and pb.HasField("last_update_at")

    def test_to_pb2_unset_optionals_stay_unset(self):
        pb = LearnedState().to_pb2()
        assert not pb.HasField("mer_estimate_kcal") and not pb.HasField("last_update_at")


def classify(**kw):
    from calorie_agent.config.config import DEFAULT_AGENT_CONFIG
    agent = CalorieAgent(DEFAULT_AGENT_CONFIG)
    agent._init_request(make_request(**kw))
    prev = agent.previous_record
    return agent.classifier._classify(prev.bcs if prev else None)


class TestClassification:
    def test_ideal(self):
        c = classify(weight=21, bcs=5)
        assert (c.state, c.conflict, c.contradiction) == (BodyState.IDEAL, False, False)
        assert c.target_weight_kg == 21

    def test_overweight_weight_and_bcs_agree_targets_midpoint(self):
        c = classify(weight=30, bcs=9)
        assert c.state is BodyState.OVERWEIGHT and not c.conflict
        assert c.target_weight_kg == 21.0

    def test_underweight_agree(self):
        c = classify(weight=15, bcs=1)
        assert c.state is BodyState.UNDERWEIGHT and not c.conflict and c.target_weight_kg == 21.0

    def test_bcs_overweight_but_weight_in_range_is_conflict_targets_min(self):
        c = classify(weight=22, bcs=9, history=[week(1, 22, bcs=9)])  # prethodni BCS potvrđuje
        assert c.state is BodyState.OVERWEIGHT and c.conflict and not c.bcs_unconfirmed
        assert c.target_weight_kg == 18.0

    def test_bcs_underweight_but_weight_in_range_targets_max(self):
        c = classify(weight=20, bcs=1, history=[week(1, 20, bcs=1)])
        assert c.state is BodyState.UNDERWEIGHT and c.conflict and c.target_weight_kg == 24.0

    def test_unconfirmed_bcs_is_ignored_in_favour_of_weight(self):
        c = classify(weight=22, bcs=9, history=[week(1, 22, bcs=5)])
        assert c.state is BodyState.IDEAL and c.bcs_unconfirmed

    def test_first_ever_conflicting_bcs_is_unconfirmed(self):
        c = classify(weight=22, bcs=9)
        assert c.state is BodyState.IDEAL and c.bcs_unconfirmed

    @pytest.mark.parametrize("weight,bcs", [(15, 9), (30, 1)])
    def test_contradiction(self, weight, bcs):
        c = classify(weight=weight, bcs=bcs)
        assert c.contradiction and c.state is BodyState.IDEAL

    def test_weight_drives_overweight_when_bcs_ideal(self):
        c = classify(weight=30, bcs=5)
        assert c.state is BodyState.OVERWEIGHT and not c.conflict and c.bcs_unconfirmed
        assert c.target_weight_kg == 21.0
