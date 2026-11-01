from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CalorieAgentRequest(_message.Message):
    __slots__ = ("dogId", "lastWeights")
    DOGID_FIELD_NUMBER: _ClassVar[int]
    LASTWEIGHTS_FIELD_NUMBER: _ClassVar[int]
    dogId: int
    lastWeights: _containers.RepeatedCompositeFieldContainer[LastWeight]
    def __init__(self, dogId: _Optional[int] = ..., lastWeights: _Optional[_Iterable[_Union[LastWeight, _Mapping]]] = ...) -> None: ...

class LastWeight(_message.Message):
    __slots__ = ("weight", "measuredAt", "bcs")
    WEIGHT_FIELD_NUMBER: _ClassVar[int]
    MEASUREDAT_FIELD_NUMBER: _ClassVar[int]
    BCS_FIELD_NUMBER: _ClassVar[int]
    weight: float
    measuredAt: str
    bcs: int
    def __init__(self, weight: _Optional[float] = ..., measuredAt: _Optional[str] = ..., bcs: _Optional[int] = ...) -> None: ...

class CalorieAgentResponse(_message.Message):
    __slots__ = ("dogId",)
    DOGID_FIELD_NUMBER: _ClassVar[int]
    dogId: int
    def __init__(self, dogId: _Optional[int] = ...) -> None: ...
