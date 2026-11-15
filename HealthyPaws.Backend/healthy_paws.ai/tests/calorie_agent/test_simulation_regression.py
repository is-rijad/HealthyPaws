"""Regresija na simulaciji (calorie_agent_simulation): agent ne smije pobjeći u ekstrem ni sa šumnijom vagom od pretpostavke."""
import pytest

from calorie_agent_simulation import harness, scenarios


@pytest.mark.parametrize("seed", range(1, 11))
def test_noisy_scale_does_not_run_away(seed):
    # Prije adaptivne procjene šuma vage: seed 1 -> 7.05 kg (pas ispod 9), seed 2 -> 14.39 kg (udebljao se sa 12.5).
    scenario = scenarios.noisy_scale_dog()
    scenario.seed = seed
    trace = harness.run(scenario)
    weights = [week.true_weight for week in trace.weeks]
    assert min(weights) > 8.5, "pas ne smije pasti daleko ispod ciljnog raspona"
    assert max(weights) < 13.5, "debeo pas se ne smije udebljati"


def test_ideal_dog_has_no_false_alarms():
    trace = harness.run(scenarios.ideal_dog())
    assert trace.count("ESCALATE") == 0


@pytest.mark.parametrize("seed", range(1, 6))
def test_sick_dog_is_escalated_and_loss_is_halted(seed):
    # Prije: eskalacija je zamrzavala plan i pas je padao na ~6.5 kg. Sada se doza podiže prema procijenjenoj potrošnji.
    scenario = scenarios.sick_dog()
    scenario.seed = seed
    trace = harness.run(scenario)
    first = trace.first_action("ESCALATE", after_week=7)
    assert first is not None and first.week - 8 <= 4, "bolest se mora otkriti u roku od ~4 sedmice"
    assert first.reasons[0] == "UNEXPLAINED_WEIGHT_LOSS"
    assert min(week.true_weight for week in trace.weeks) > 7.3


@pytest.mark.parametrize("seed", range(1, 9))
def test_underweight_dog_reaches_range_without_overshoot(seed):
    # max_gain_pct_per_week=1.0 -> 5/20 pasa ne dosegne raspon u 40 sedmica; 1.5 -> svi.
    scenario = scenarios.underweight_dog()
    scenario.seed = seed
    trace = harness.run(scenario)
    weights = [week.true_weight for week in trace.weeks]
    assert max(weights) <= scenario.max_weight_kg, "ne smije prestići gornju granicu"
    assert max(weights) >= scenario.min_weight_kg, "mora dosegnuti donju granicu raspona"
    assert trace.count("ESCALATE") == 0
