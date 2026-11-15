"""Zajednički helperi za testove: builder gRPC zahtjeva za calorie agenta."""
from datetime import datetime, timedelta, timezone

import pytest
from google.protobuf.timestamp_pb2 import Timestamp

from calorie_agent.calorie_agent import CalorieAgent
from calorie_agent.config.config import CalorieAgentConfig
from rpc.service.calorie_agent import calorie_agent_pb2 as pb

T0 = datetime(2026, 1, 1, 8, 0, tzinfo=timezone.utc)


def ts(dt: datetime) -> Timestamp:
    t = Timestamp()
    t.FromDatetime(dt)
    return t


def week(weeks_ago: float, weight: float, bcs: int | None = 5, now: datetime = T0) -> pb.WeekRecord:
    record = pb.WeekRecord(measured_at=ts(now - timedelta(weeks=weeks_ago)), weight_kg=weight)
    if bcs is not None:
        record.bcs = bcs
    return record


def make_request(*, weight=20.0, bcs=5, min_weight=18.0, max_weight=24.0, now=T0, dog_id=1,
                 history=(), intake="default", plan="none", neutered=True, activity="1.2",
                 life_stage="adult", pregnant=False, food_kcal=3.5, correlation_id="cid-1"):
    """intake: "default" = ispravan unos, None = bez unosa, ili gotov pb.IntakeSummary.
    plan: "none" = bez plana, ili gotov pb.PlanState (vidi make_plan)."""
    request = pb.CalorieAgentRequest(
        correlation_id=correlation_id,
        measurement=pb.Measurement(dog_id=dog_id, weight=weight, bcs=bcs, min_weight=min_weight,
                                   max_weight=max_weight, measured_at=ts(now)),
        profile=pb.DogProfile(life_stage=life_stage, neutered=neutered, activity_factor=activity,
                              pregnant_or_lactating=pregnant, food_kcal_per_gram=food_kcal),
    )
    request.history.extend(history)
    if intake == "default":
        intake = make_intake()
    if intake is not None:
        request.intake.CopyFrom(intake)
    if plan != "none" and plan is not None:
        request.plan.CopyFrom(plan)
    return request


def make_intake(eaten=350.0, offered=380.0, days_covered=7.0, period_days=7.0, treat_kcal=None):
    intake = pb.IntakeSummary(days_covered=days_covered, period_days=period_days)
    if eaten is not None:
        intake.eaten_g_per_day = eaten
    if offered is not None:
        intake.offered_g_per_day = offered
    if treat_kcal is not None:
        intake.treat_kcal_per_day = treat_kcal
    return intake


def make_plan(kcal=1250.0, version=1, **learned_kwargs):
    plan = pb.PlanState(plan_version=version)
    if kcal is not None:
        plan.current_daily_kcal = kcal
    for key, value in learned_kwargs.items():
        if key == "last_update_at":
            plan.learned.last_update_at.CopyFrom(ts(value))
        else:
            setattr(plan.learned, key, value)
    return plan


def run_agent(request, config: CalorieAgentConfig | None = None):
    return CalorieAgent(config or CalorieAgentConfig()).run(request)


def action_name(response) -> str:
    return pb.Action.Name(response.action)


@pytest.fixture
def config() -> CalorieAgentConfig:
    return CalorieAgentConfig()
