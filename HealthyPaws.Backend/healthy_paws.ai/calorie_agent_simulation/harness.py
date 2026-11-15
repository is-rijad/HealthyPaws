"""Harness glumi .NET stranu: drži historiju, plan i naučeno stanje, sastavlja
zahtjev, šalje ga agentu (preko JSON-a, kao u produkciji) i primjenjuje odluku
po pravilima ugovora:

  * `learned` iz SVAKOG odgovora se sprema,
  * plan se primjenjuje samo ako je `planChanged`,
  * mjerenje na koje je agent odgovorio RequestRemeasure NE ulazi u historiju
    (vlasnik ponovo vaga isti dan).
"""
from __future__ import annotations

import random
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from typing import Callable, Optional

from google.protobuf.json_format import MessageToDict
from google.protobuf.timestamp_pb2 import Timestamp

from calorie_agent.calorie_agent import CalorieAgent
from calorie_agent.config.config import CalorieAgentConfig
from calorie_agent.core.calculations import Calculations
from calorie_agent.interfaces.dog_profile import DogProfile
from rpc.service.calorie_agent import calorie_agent_pb2 as pb
from .virtual_dog import VirtualDog

DEFAULT_CONFIG = CalorieAgentConfig()
KCAL_PER_GRAM = 3.5
T0 = datetime(2026, 1, 5, 8, 0, tzinfo=timezone.utc)


@dataclass
class Scenario:
    name: str
    description: str
    start_weight_kg: float
    min_weight_kg: float
    max_weight_kg: float
    mer_ratio: float = 1.0               # skrivena potrošnja / formulni prior pri početnoj težini
    weeks: int = 40
    seed: int = 1
    neutered: bool = True
    weight_noise_kg: float = 0.15
    scale_resolution_kg: float = 0.1     # kućna vaga
    bcs_flip_prob: float = 0.0           # vlasnik pogriješi BCS za ±1 korak (±2)
    # skrivene promjene psa u zadanoj sedmici: {sedmica: {atribut: vrijednost}}
    events: dict[int, dict[str, float]] = field(default_factory=dict)
    # vlasnik pogriješi unos težine: {sedmica: prijavljena_težina}
    typos: dict[int, float] = field(default_factory=dict)
    missed_weeks: set[int] = field(default_factory=set)

    @property
    def ideal_kg(self) -> float:
        return (self.min_weight_kg + self.max_weight_kg) / 2.0


@dataclass
class WeekTrace:
    week: int
    measured_at: datetime
    true_weight: float
    reported_weight: float
    bcs: int
    plan_kcal: Optional[float]
    action: str
    reasons: tuple[str, ...]
    mer_estimate: Optional[float]
    true_mer: float
    plan_changed: bool
    new_kcal: Optional[float]
    requires_review: bool
    floor_kcal: Optional[float]
    ceiling_kcal: Optional[float]


@dataclass
class Trace:
    scenario: Scenario
    weeks: list[WeekTrace]
    decisions: list[pb.CalorieAgentResponse]
    remeasures: int = 0          # koliko puta je agent tražio ponovno vaganje

    def first_action(self, action: str, after_week: int = -1) -> Optional[WeekTrace]:
        for w in self.weeks:
            if w.week > after_week and w.action == action:
                return w
        return None

    def plan_series(self) -> list[float]:
        return [w.plan_kcal for w in self.weeks if w.plan_kcal is not None]

    def count(self, action: str) -> int:
        return sum(1 for w in self.weeks if w.action == action)


def _ts(dt: datetime) -> Timestamp:
    t = Timestamp()
    t.FromDatetime(dt)
    return t


def _build_request(cid, dog_id, reported, bcs, scenario, measured_at, history, intake, plan_kcal,
                   plan_version, learned) -> pb.CalorieAgentRequest:
    req = pb.CalorieAgentRequest(
        correlation_id=cid,
        measurement=pb.Measurement(dog_id=dog_id, weight=reported, bcs=bcs, min_weight=scenario.min_weight_kg,
                                   max_weight=scenario.max_weight_kg, measured_at=_ts(measured_at)),
        profile=pb.DogProfile(life_stage="adult", neutered=scenario.neutered, activity_factor="1.2",
                              pregnant_or_lactating=False, food_kcal_per_gram=KCAL_PER_GRAM))
    req.history.extend(history)
    if intake is not None:
        req.intake.CopyFrom(intake)
    if plan_kcal is not None:
        req.plan.CopyFrom(pb.PlanState(current_daily_kcal=plan_kcal, plan_version=plan_version))
        if learned is not None:
            req.plan.learned.CopyFrom(learned)
    elif learned is not None:
        req.plan.learned.CopyFrom(learned)
        req.plan.plan_version = plan_version
    return req


def run(scenario: Scenario, cfg: CalorieAgentConfig = DEFAULT_CONFIG, *,
        agent: Callable[[pb.CalorieAgentRequest], pb.CalorieAgentResponse] | None = None) -> Trace:
    rng = random.Random(scenario.seed)
    calc = Calculations(cfg)
    profile = DogProfile(life_stage="adult", neutered=scenario.neutered, activity_factor="1.2",
                         pregnant_or_lactating=False, food_kcal_per_gram=KCAL_PER_GRAM)
    prior0 = calc.mer_prior(scenario.start_weight_kg, profile)
    dog = VirtualDog(weight_kg=scenario.start_weight_kg, start_weight_kg=scenario.start_weight_kg,
                     mer_at_start_kcal=prior0 * scenario.mer_ratio)

    plan_kcal: Optional[float] = None
    plan_version = 0
    learned: Optional[pb.LearnedState] = None
    history: list[pb.WeekRecord] = []
    eaten_sum = offered_sum = 0.0
    days_since = 0

    weeks: list[WeekTrace] = []
    decisions: list[pb.CalorieAgentResponse] = []
    remeasures = 0
    run_agent = agent or (lambda r: CalorieAgent(cfg).run(r))

    for week in range(scenario.weeks):
        for attr, value in scenario.events.get(week, {}).items():
            setattr(dog, attr, value)

        if week > 0:
            for _ in range(7):
                offered = plan_kcal if plan_kcal is not None else 0.0
                eaten_sum += dog.step_day(offered)
                offered_sum += offered
                days_since += 1

        if week in scenario.missed_weeks:
            continue

        t = T0 + timedelta(days=7 * week)
        attempts = 0
        while True:
            attempts += 1
            reported = round((dog.weight_kg + rng.gauss(0.0, scenario.weight_noise_kg)) / scenario.scale_resolution_kg) \
                * scenario.scale_resolution_kg
            if attempts == 1 and week in scenario.typos:
                reported = scenario.typos[week]
            bcs = dog.true_bcs(scenario.ideal_kg)
            if rng.random() < scenario.bcs_flip_prob:
                bcs = min(9, max(1, bcs + rng.choice((-2, 2))))

            measured_at = t + timedelta(minutes=attempts - 1)
            intake = None
            if days_since > 0 and plan_kcal is not None:
                intake = pb.IntakeSummary(days_covered=float(days_since), period_days=float(days_since),
                                          eaten_g_per_day=eaten_sum / days_since / KCAL_PER_GRAM,
                                          offered_g_per_day=offered_sum / days_since / KCAL_PER_GRAM)

            req = _build_request(f"{scenario.name}-{week}-{attempts}", 1, reported, bcs, scenario, measured_at,
                                 history[-8:], intake, plan_kcal, plan_version, learned)
            resp = run_agent(req)

            # kao backend (HandleResponse): has_error = nevaljan ulaz, ništa se ne sprema
            if not resp.has_error:
                learned = resp.learned
            if resp.action == pb.REQUEST_REMEASURE and attempts < 3:
                remeasures += 1
                continue  # vlasnik ponovo vaga; ovo mjerenje ne ulazi u historiju
            break

        decisions.append(resp)
        action = pb.Action.Name(resp.action)
        if resp.action != pb.REQUEST_REMEASURE and not resp.has_error:
            rec = pb.WeekRecord(measured_at=_ts(measured_at), weight_kg=reported, bcs=bcs)
            if intake is not None:
                rec.eaten_g_per_day = intake.eaten_g_per_day
                rec.offered_g_per_day = intake.offered_g_per_day
            history.append(rec)
            eaten_sum = offered_sum = 0.0
            days_since = 0

        new_kcal = resp.new_daily_kcal if resp.HasField("new_daily_kcal") else None
        if resp.plan_changed and new_kcal is not None and not resp.has_error:
            plan_kcal = new_kcal
            plan_version += 1

        diag = MessageToDict(resp.diagnostics)
        weeks.append(WeekTrace(
            week=week, measured_at=measured_at, true_weight=dog.weight_kg, reported_weight=reported, bcs=bcs,
            plan_kcal=plan_kcal, action=action, reasons=tuple(resp.reason_codes),
            mer_estimate=resp.learned.mer_estimate_kcal if resp.learned.HasField("mer_estimate_kcal") else None,
            true_mer=dog.true_mer(), plan_changed=resp.plan_changed, new_kcal=new_kcal,
            requires_review=resp.requires_human_review,
            floor_kcal=diag.get("floor_kcal"), ceiling_kcal=diag.get("ceiling_kcal")))

    return Trace(scenario, weeks, decisions, remeasures)
