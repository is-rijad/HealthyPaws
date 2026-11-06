from enum import Enum


class BodyState(str, Enum):
    UNDERWEIGHT = "Underweight"
    IDEAL = "Ideal"
    OVERWEIGHT = "Overweight"
