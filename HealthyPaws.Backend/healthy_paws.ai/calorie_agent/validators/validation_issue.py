from dataclasses import dataclass

from calorie_agent.validators.severity import Severity


@dataclass(frozen=True)
class ValidationIssue:
    severity: Severity
    field: str
    detail: str
    confirmable: bool = False

    def to_dict(self) -> dict:
        return {"severity": self.severity.value, "field": self.field, "detail": self.detail,
                "confirmable": self.confirmable}