"""Simulacija: agent + "pas" s poznatom stvarnom potrošnjom, tjedan po tjedan (izlaz -> ulaz sljedećeg tjedna)."""
from datetime import timedelta

import numpy as np
import pytest

from rpc.service.calorie_agent import calorie_agent_pb2 as pb
from tests.conftest import T0, action_name, make_intake, make_request, run_agent, ts

FOOD = 3.5


class SimulatedDog:
    def __init__(self, weight, true_mer, bcs_of, noise_sd=0.0, seed=0, **request_kwargs):
        self.weight, self.true_mer, self.bcs_of, self.noise_sd = weight, true_mer, bcs_of, noise_sd
        self.rng = np.random.default_rng(seed)
        self.kwargs = request_kwargs
        self.plan_kcal, self.plan_version, self.learned = None, 0, None
        self.history: list[pb.WeekRecord] = []
        self.responses: list[pb.CalorieAgentResponse] = []
        self.week = 0

    def _measure(self):
        return self.weight + self.rng.normal(0, self.noise_sd)

    def tick(self):
        now = T0 + timedelta(weeks=self.week)
        measured = self._measure()
        bcs = self.bcs_of(self.weight)
        plan = None
        intake = None
        if self.plan_kcal is not None:
            plan = pb.PlanState(current_daily_kcal=self.plan_kcal, plan_version=self.plan_version)
            if self.learned is not None:
                plan.learned.CopyFrom(self.learned)
            offered = self.plan_kcal / FOOD
            intake = make_intake(eaten=offered, offered=offered)  # pas pojede sve što dobije
        request = make_request(weight=measured, bcs=bcs, now=now, history=list(self.history), plan=plan,
                               intake=intake, food_kcal=FOOD, **self.kwargs)
        response = run_agent(request)
        self.responses.append(response)

        # backend: primijeni plan i naučeno stanje samo kad nema greške (kao u HandleResponse)
        if not response.has_error:
            self.learned = response.learned
            if response.plan_changed and response.HasField("new_daily_kcal"):
                self.plan_kcal, self.plan_version = response.new_daily_kcal, self.plan_version + 1
        record = pb.WeekRecord(measured_at=ts(now), weight_kg=measured, bcs=bcs)
        self.history = (self.history + [record])[-8:]

        # fizika: sedmicu dana jede plan (nakon odluke), troši true_mer
        if self.plan_kcal is not None:
            self.weight += (self.plan_kcal - self.true_mer) * 7 / 7000.0
        self.week += 1
        return response


def bcs_for(min_w, max_w):
    def f(weight):
        return 9 if weight > max_w * 1.1 else 7 if weight > max_w else 5 if weight >= min_w else 3
    return f


class TestClosedLoop:
    def test_overweight_dog_loses_weight_safely(self):
        dog = SimulatedDog(30.0, 1500.0, bcs_for(18, 24), min_weight=18, max_weight=24)
        for _ in range(16):
            dog.tick()
        actions = [action_name(r) for r in dog.responses]

        assert actions[0] == "SET_INITIAL"
        assert dog.weight < 30.0 - 2.0, "pas mora osjetno smršaviti"
        assert dog.weight > 24.0 - 3.0, "ali ne smije pasti prekomjerno ispod ciljnog raspona"
        assert "ESCALATE" not in actions, [list(r.reason_codes) for r in dog.responses]
        assert len(dog.responses) == 16

    def test_plan_changes_respect_step_limits_every_week(self):
        dog = SimulatedDog(30.0, 1500.0, bcs_for(18, 24), min_weight=18, max_weight=24)
        prev = None
        for _ in range(16):
            r = dog.tick()
            if r.HasField("new_daily_kcal") and prev is not None and r.plan_changed:
                assert abs(r.new_daily_kcal / prev - 1.0) <= 0.10 + 1e-9
            if r.HasField("new_daily_kcal"):
                prev = r.new_daily_kcal

    def test_plan_always_inside_safe_corridor(self):
        dog = SimulatedDog(30.0, 1500.0, bcs_for(18, 24), min_weight=18, max_weight=24)
        for _ in range(16):
            r = dog.tick()
            if r.HasField("new_daily_kcal"):
                d = {f: v for f, v in r.diagnostics.fields.items()}
                if "floor_kcal" in d:
                    assert d["floor_kcal"].number_value - 1e-6 <= r.new_daily_kcal <= d["ceiling_kcal"].number_value + 1e-6

    def test_dog_at_maintenance_is_left_alone(self):
        true_mer = 70 * 21 ** 0.75 * 1.6 * 1.2
        dog = SimulatedDog(21.0, true_mer, bcs_for(18, 24), min_weight=18, max_weight=24)
        for _ in range(10):
            dog.tick()
        actions = [action_name(r) for r in dog.responses]
        assert actions[0] == "SET_INITIAL"
        assert "ESCALATE" not in actions and "INCREASE" not in actions and "DECREASE" not in actions
        assert dog.weight == pytest.approx(21.0, abs=0.05)

    def test_underweight_dog_gains_weight(self):
        dog = SimulatedDog(15.0, 1000.0, bcs_for(18, 24), min_weight=18, max_weight=24)
        for _ in range(12):
            dog.tick()
        assert dog.weight > 15.0 + 0.5
        assert "ESCALATE" not in [action_name(r) for r in dog.responses]

    def test_estimate_converges_toward_true_mer(self):
        dog = SimulatedDog(30.0, 1500.0, bcs_for(18, 24), min_weight=18, max_weight=24)
        for _ in range(16):
            dog.tick()
        learned = [r.learned.mer_estimate_kcal for r in dog.responses if r.learned.n_updates > 0]
        assert learned, "agent mora učiti"
        # formula za 30 kg daje ~1722; stvarna je 1500 -> procjena se mora približiti stvarnoj
        prior = 70 * 30 ** 0.75 * 1.6 * 1.2
        assert abs(learned[-1] - 1500.0) < abs(prior - 1500.0)

    def test_noisy_scale_does_not_cause_escalations_or_oscillation(self):
        dog = SimulatedDog(21.0, 70 * 21 ** 0.75 * 1.6 * 1.2, bcs_for(18, 24), noise_sd=0.1, seed=3,
                           min_weight=18, max_weight=24)
        for _ in range(12):
            dog.tick()
        actions = [action_name(r) for r in dog.responses]
        assert actions.count("ESCALATE") == 0, [(action_name(r), list(r.reason_codes)) for r in dog.responses]
        assert actions.count("INCREASE") + actions.count("DECREASE") <= 3  # nema treperenja doze zbog šuma

    def test_dog_eating_outside_feeder_triggers_escalation_not_endless_cutting(self):
        """Pas dobija 600 kcal/dan hrane više nego što hranilica vidi: težina raste uprkos 'dijeti'."""
        dog = SimulatedDog(30.0, 1500.0, bcs_for(18, 24), min_weight=18, max_weight=24)
        for week in range(14):
            r = dog.tick()
            dog.weight += 600 * 7 / 7000.0 if dog.plan_kcal else 0.0  # skrivena hrana
        assert "ESCALATE" in [action_name(r) for r in dog.responses], "pas se debljao 14 sedmica bez eskalacije"
        codes = {c for r in dog.responses for c in r.reason_codes}
        assert codes & {"MODEL_ERROR_LARGE", "MODEL_MISMATCH_PERSISTENT", "ESTIMATE_FAR_FROM_PRIOR",
                        "PLAN_AT_BOUND_NO_PROGRESS"}, "skrivena hrana mora biti prepoznata"
