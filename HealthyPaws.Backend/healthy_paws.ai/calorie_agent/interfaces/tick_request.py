from dataclasses import dataclass
from typing import Optional

from calorie_agent.interfaces.helpers import optional_field, to_utc_datetime
from calorie_agent.interfaces.learned_state import LearnedState
from calorie_agent.interfaces.dog_profile import DogProfile
from calorie_agent.interfaces.intake_summary import IntakeSummary
from calorie_agent.interfaces.measurement import Measurement
from calorie_agent.interfaces.plan_state import PlanState
from calorie_agent.interfaces.week_record import WeekRecord
from rpc.service.calorie_agent import calorie_agent_pb2 as pb
from rpc.service.calorie_agent.calorie_agent_pb2 import CalorieAgentRequest

@dataclass(frozen=True)
class TickRequest:
    correlation_id: str
    measurement: Measurement
    history: tuple[WeekRecord, ...]
    intake: Optional[IntakeSummary]
    profile: DogProfile
    plan: Optional[PlanState]

    @classmethod
    def serialize(cls, proto_request: CalorieAgentRequest) -> "TickRequest":
        measurement = proto_request.measurement
        intake = proto_request.intake
        profile = proto_request.profile
        plan = proto_request.plan

        return TickRequest(
            correlation_id=proto_request.correlation_id,
            measurement=Measurement(dog_id=measurement.dog_id, weight=measurement.weight, bcs=measurement.bcs,
                                    min_weight=measurement.min_weight, max_weight=measurement.max_weight,
                                    measured_at=to_utc_datetime(measurement.measured_at)),
            history=tuple(WeekRecord(measured_at=to_utc_datetime(record.measured_at), weight_kg=record.weight_kg,
                                     bcs=optional_field(record, "bcs"),
                                     eaten_g_per_day=optional_field(record, "eaten_g_per_day"),
                                     offered_g_per_day=optional_field(record, "offered_g_per_day"))
                          for record in proto_request.history),
            intake=IntakeSummary(eaten_g_per_day=optional_field(intake, "eaten_g_per_day"),
                                 offered_g_per_day=optional_field(intake, "offered_g_per_day"),
                                 days_covered=intake.days_covered, period_days=intake.period_days,
                                 treat_kcal_per_day=optional_field(intake, "treat_kcal_per_day"))
            if proto_request.HasField("intake") else None,
            profile=DogProfile(life_stage=profile.life_stage, neutered=profile.neutered,
                               activity_factor=profile.activity_factor,
                               pregnant_or_lactating=profile.pregnant_or_lactating,
                               food_kcal_per_gram=profile.food_kcal_per_gram),
            plan=PlanState(current_daily_kcal=optional_field(plan, "current_daily_kcal"),
                           plan_version=plan.plan_version,
                           learned=cls._learned(plan.learned))
            if proto_request.HasField("plan") else None,
        )

    @classmethod
    def _learned(cls, message: pb.LearnedState) -> LearnedState:
        return LearnedState(
            mer_estimate_kcal=optional_field(message, "mer_estimate_kcal"),
            variance_kcal2=optional_field(message, "variance_kcal2"),
            n_updates=message.n_updates,
            consecutive_bad_weeks=message.consecutive_bad_weeks,
            weeks_at_bound=message.weeks_at_bound,
            pending_weight_kg=optional_field(message, "pending_weight_kg"),
            last_update_at=to_utc_datetime(message.last_update_at) if message.HasField("last_update_at") else None,
        )
