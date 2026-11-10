import datetime

from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import struct_pb2 as _struct_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class Action(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ACTION_UNSPECIFIED: _ClassVar[Action]
    NO_CHANGE: _ClassVar[Action]
    INCREASE: _ClassVar[Action]
    DECREASE: _ClassVar[Action]
    SET_INITIAL: _ClassVar[Action]
    ESCALATE: _ClassVar[Action]
    REQUEST_REMEASURE: _ClassVar[Action]
ACTION_UNSPECIFIED: Action
NO_CHANGE: Action
INCREASE: Action
DECREASE: Action
SET_INITIAL: Action
ESCALATE: Action
REQUEST_REMEASURE: Action

class Measurement(_message.Message):
    __slots__ = ("dog_id", "weight", "bcs", "min_weight", "max_weight", "measured_at")
    DOG_ID_FIELD_NUMBER: _ClassVar[int]
    WEIGHT_FIELD_NUMBER: _ClassVar[int]
    BCS_FIELD_NUMBER: _ClassVar[int]
    MIN_WEIGHT_FIELD_NUMBER: _ClassVar[int]
    MAX_WEIGHT_FIELD_NUMBER: _ClassVar[int]
    MEASURED_AT_FIELD_NUMBER: _ClassVar[int]
    dog_id: int
    weight: float
    bcs: int
    min_weight: float
    max_weight: float
    measured_at: _timestamp_pb2.Timestamp
    def __init__(self, dog_id: _Optional[int] = ..., weight: _Optional[float] = ..., bcs: _Optional[int] = ..., min_weight: _Optional[float] = ..., max_weight: _Optional[float] = ..., measured_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ...) -> None: ...

class WeekRecord(_message.Message):
    __slots__ = ("measured_at", "weight_kg", "bcs", "eaten_g_per_day", "offered_g_per_day")
    MEASURED_AT_FIELD_NUMBER: _ClassVar[int]
    WEIGHT_KG_FIELD_NUMBER: _ClassVar[int]
    BCS_FIELD_NUMBER: _ClassVar[int]
    EATEN_G_PER_DAY_FIELD_NUMBER: _ClassVar[int]
    OFFERED_G_PER_DAY_FIELD_NUMBER: _ClassVar[int]
    measured_at: _timestamp_pb2.Timestamp
    weight_kg: float
    bcs: int
    eaten_g_per_day: float
    offered_g_per_day: float
    def __init__(self, measured_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., weight_kg: _Optional[float] = ..., bcs: _Optional[int] = ..., eaten_g_per_day: _Optional[float] = ..., offered_g_per_day: _Optional[float] = ...) -> None: ...

class IntakeSummary(_message.Message):
    __slots__ = ("eaten_g_per_day", "offered_g_per_day", "days_covered", "period_days", "treat_kcal_per_day")
    EATEN_G_PER_DAY_FIELD_NUMBER: _ClassVar[int]
    OFFERED_G_PER_DAY_FIELD_NUMBER: _ClassVar[int]
    DAYS_COVERED_FIELD_NUMBER: _ClassVar[int]
    PERIOD_DAYS_FIELD_NUMBER: _ClassVar[int]
    TREAT_KCAL_PER_DAY_FIELD_NUMBER: _ClassVar[int]
    eaten_g_per_day: float
    offered_g_per_day: float
    days_covered: float
    period_days: float
    treat_kcal_per_day: float
    def __init__(self, eaten_g_per_day: _Optional[float] = ..., offered_g_per_day: _Optional[float] = ..., days_covered: _Optional[float] = ..., period_days: _Optional[float] = ..., treat_kcal_per_day: _Optional[float] = ...) -> None: ...

class DogProfile(_message.Message):
    __slots__ = ("life_stage", "neutered", "activity_factor", "pregnant_or_lactating", "food_kcal_per_gram")
    LIFE_STAGE_FIELD_NUMBER: _ClassVar[int]
    NEUTERED_FIELD_NUMBER: _ClassVar[int]
    ACTIVITY_FACTOR_FIELD_NUMBER: _ClassVar[int]
    PREGNANT_OR_LACTATING_FIELD_NUMBER: _ClassVar[int]
    FOOD_KCAL_PER_GRAM_FIELD_NUMBER: _ClassVar[int]
    life_stage: str
    neutered: bool
    activity_factor: str
    pregnant_or_lactating: bool
    food_kcal_per_gram: float
    def __init__(self, life_stage: _Optional[str] = ..., neutered: _Optional[bool] = ..., activity_factor: _Optional[str] = ..., pregnant_or_lactating: _Optional[bool] = ..., food_kcal_per_gram: _Optional[float] = ...) -> None: ...

class LearnedState(_message.Message):
    __slots__ = ("mer_estimate_kcal", "variance_kcal2", "n_updates", "consecutive_bad_weeks", "weeks_at_bound", "pending_weight_kg", "last_update_at")
    MER_ESTIMATE_KCAL_FIELD_NUMBER: _ClassVar[int]
    VARIANCE_KCAL2_FIELD_NUMBER: _ClassVar[int]
    N_UPDATES_FIELD_NUMBER: _ClassVar[int]
    CONSECUTIVE_BAD_WEEKS_FIELD_NUMBER: _ClassVar[int]
    WEEKS_AT_BOUND_FIELD_NUMBER: _ClassVar[int]
    PENDING_WEIGHT_KG_FIELD_NUMBER: _ClassVar[int]
    LAST_UPDATE_AT_FIELD_NUMBER: _ClassVar[int]
    mer_estimate_kcal: float
    variance_kcal2: float
    n_updates: int
    consecutive_bad_weeks: int
    weeks_at_bound: int
    pending_weight_kg: float
    last_update_at: _timestamp_pb2.Timestamp
    def __init__(self, mer_estimate_kcal: _Optional[float] = ..., variance_kcal2: _Optional[float] = ..., n_updates: _Optional[int] = ..., consecutive_bad_weeks: _Optional[int] = ..., weeks_at_bound: _Optional[int] = ..., pending_weight_kg: _Optional[float] = ..., last_update_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ...) -> None: ...

class PlanState(_message.Message):
    __slots__ = ("current_daily_kcal", "plan_version", "learned")
    CURRENT_DAILY_KCAL_FIELD_NUMBER: _ClassVar[int]
    PLAN_VERSION_FIELD_NUMBER: _ClassVar[int]
    LEARNED_FIELD_NUMBER: _ClassVar[int]
    current_daily_kcal: float
    plan_version: int
    learned: LearnedState
    def __init__(self, current_daily_kcal: _Optional[float] = ..., plan_version: _Optional[int] = ..., learned: _Optional[_Union[LearnedState, _Mapping]] = ...) -> None: ...

class CalorieAgentRequest(_message.Message):
    __slots__ = ("correlation_id", "measurement", "history", "intake", "profile", "plan")
    CORRELATION_ID_FIELD_NUMBER: _ClassVar[int]
    MEASUREMENT_FIELD_NUMBER: _ClassVar[int]
    HISTORY_FIELD_NUMBER: _ClassVar[int]
    INTAKE_FIELD_NUMBER: _ClassVar[int]
    PROFILE_FIELD_NUMBER: _ClassVar[int]
    PLAN_FIELD_NUMBER: _ClassVar[int]
    correlation_id: str
    measurement: Measurement
    history: _containers.RepeatedCompositeFieldContainer[WeekRecord]
    intake: IntakeSummary
    profile: DogProfile
    plan: PlanState
    def __init__(self, correlation_id: _Optional[str] = ..., measurement: _Optional[_Union[Measurement, _Mapping]] = ..., history: _Optional[_Iterable[_Union[WeekRecord, _Mapping]]] = ..., intake: _Optional[_Union[IntakeSummary, _Mapping]] = ..., profile: _Optional[_Union[DogProfile, _Mapping]] = ..., plan: _Optional[_Union[PlanState, _Mapping]] = ...) -> None: ...

class CalorieAgentResponse(_message.Message):
    __slots__ = ("dog_id", "measured_at", "correlation_id", "plan_version", "action", "plan_changed", "new_daily_kcal", "new_daily_grams", "reason_codes", "explanation", "requires_human_review", "next_evaluation_at", "learned", "diagnostics")
    DOG_ID_FIELD_NUMBER: _ClassVar[int]
    MEASURED_AT_FIELD_NUMBER: _ClassVar[int]
    CORRELATION_ID_FIELD_NUMBER: _ClassVar[int]
    PLAN_VERSION_FIELD_NUMBER: _ClassVar[int]
    ACTION_FIELD_NUMBER: _ClassVar[int]
    PLAN_CHANGED_FIELD_NUMBER: _ClassVar[int]
    NEW_DAILY_KCAL_FIELD_NUMBER: _ClassVar[int]
    NEW_DAILY_GRAMS_FIELD_NUMBER: _ClassVar[int]
    REASON_CODES_FIELD_NUMBER: _ClassVar[int]
    EXPLANATION_FIELD_NUMBER: _ClassVar[int]
    REQUIRES_HUMAN_REVIEW_FIELD_NUMBER: _ClassVar[int]
    NEXT_EVALUATION_AT_FIELD_NUMBER: _ClassVar[int]
    LEARNED_FIELD_NUMBER: _ClassVar[int]
    DIAGNOSTICS_FIELD_NUMBER: _ClassVar[int]
    dog_id: int
    measured_at: _timestamp_pb2.Timestamp
    correlation_id: str
    plan_version: int
    action: Action
    plan_changed: bool
    new_daily_kcal: float
    new_daily_grams: float
    reason_codes: _containers.RepeatedScalarFieldContainer[str]
    explanation: str
    requires_human_review: bool
    next_evaluation_at: _timestamp_pb2.Timestamp
    learned: LearnedState
    diagnostics: _struct_pb2.Struct
    def __init__(self, dog_id: _Optional[int] = ..., measured_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., correlation_id: _Optional[str] = ..., plan_version: _Optional[int] = ..., action: _Optional[_Union[Action, str]] = ..., plan_changed: _Optional[bool] = ..., new_daily_kcal: _Optional[float] = ..., new_daily_grams: _Optional[float] = ..., reason_codes: _Optional[_Iterable[str]] = ..., explanation: _Optional[str] = ..., requires_human_review: _Optional[bool] = ..., next_evaluation_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., learned: _Optional[_Union[LearnedState, _Mapping]] = ..., diagnostics: _Optional[_Union[_struct_pb2.Struct, _Mapping]] = ...) -> None: ...
