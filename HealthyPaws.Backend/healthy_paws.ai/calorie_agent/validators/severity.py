from enum import Enum


class Severity(str, Enum):
    REMEASURE = "Remeasure"
    ESCALATE = "Escalate"
    WARN = "Warn"