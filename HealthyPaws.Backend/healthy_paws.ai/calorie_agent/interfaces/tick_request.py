from dataclasses import dataclass
from typing import Optional

from calorie_agent.interfaces.helpers import _opt, _dt
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
    def serialize(cls, tick_request: CalorieAgentRequest) -> "TickRequest":
        m = tick_request.measurement
        i = tick_request.intake
        p = tick_request.profile
        plan = tick_request.plan

        return TickRequest(
            correlation_id=tick_request.correlation_id,
            measurement=Measurement(dog_id=m.dog_id, weight=m.weight, bcs=m.bcs, min_weight=m.min_weight,
                                    max_weight=m.max_weight, measured_at=_dt(m.measured_at)),
            history=tuple(WeekRecord(measured_at=_dt(w.measured_at), weight_kg=w.weight_kg,
                                     bcs=_opt(w, "bcs"), eaten_g_per_day=_opt(w, "eaten_g_per_day"),
                                     offered_g_per_day=_opt(w, "offered_g_per_day"))
                          for w in tick_request.history),
            intake=IntakeSummary(eaten_g_per_day=_opt(i, "eaten_g_per_day"),
                                 offered_g_per_day=_opt(i, "offered_g_per_day"),
                                 days_covered=i.days_covered, period_days=i.period_days) if tick_request.HasField("intake") else None,
            profile=DogProfile(life_stage=p.life_stage, neutered=p.neutered, activity_factor=p.activity_factor,
                               pregnant_or_lactating=p.pregnant_or_lactating,
                               food_kcal_per_gram=p.food_kcal_per_gram),
            plan=PlanState(current_daily_kcal=_opt(plan, "current_daily_kcal"), plan_version=plan.plan_version,
                           learned=cls._learned(plan.learned)) if tick_request.HasField("plan") else None,
        )


    @classmethod
    def _learned(cls, msg: pb.LearnedState) -> LearnedState:
        return LearnedState(
            mer_estimate_kcal=_opt(msg, "mer_estimate_kcal"),
            variance_kcal2=_opt(msg, "variance_kcal2"),
            n_updates=msg.n_updates,
            consecutive_bad_weeks=msg.consecutive_bad_weeks,
            weeks_at_bound=msg.weeks_at_bound,
            pending_weight_kg=_opt(msg, "pending_weight_kg"),
            last_update_at=_dt(msg.last_update_at) if msg.HasField("last_update_at") else None,
        )