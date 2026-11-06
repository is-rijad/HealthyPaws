from enum import Enum


class Action(str, Enum):
    ACTION_UNSPECIFIED = "ActionUnspecified"
    NO_CHANGE = "NoChange"
    INCREASE = "Increase"
    DECREASE = "Decrease"
    SET_INITIAL = "SetInitial"
    ESCALATE = "Escalate"
    REQUEST_REMEASURE = "RequestRemeasure"


PLAN_CHANGING = {Action.INCREASE, Action.DECREASE, Action.SET_INITIAL}
