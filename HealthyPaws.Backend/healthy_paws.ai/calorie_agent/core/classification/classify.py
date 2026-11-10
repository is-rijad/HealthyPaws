import statistics
from typing import Optional

import calorie_agent.calorie_agent as ca
from calorie_agent.core.classification.body_state import BodyState
from calorie_agent.core.classification.classification import Classification
from calorie_agent.interfaces.action import Action


class Classify:
    def __init__(self, agent: ca.CalorieAgent):
        self.agent = agent

    def classify_and_check(self):
        previous_record = self.agent.previous_record
        classification = self._classify(previous_record.bcs if previous_record is not None else None)
        self.agent.context["classification"] = classification
        self.agent.diagnostics.update(state=classification.state.value, conflict=classification.conflict,
                                      target_weight_kg=classification.target_weight_kg)

        if classification.bcs_unconfirmed:
            self.agent.reason_codes.append("BCS_UNCONFIRMED")
        if classification.contradiction:
            return self.agent.decisions.stop(Action.ESCALATE, "CONTRADICTORY_BCS_WEIGHT")

        measurement = self.agent.measurement
        reference_weight_kg = statistics.median(
            [record.weight_kg for record in self.agent.history[-3:]] + [measurement.weight])
        breed_midpoint_kg = (measurement.min_weight + measurement.max_weight) / 2.0
        # Dozvoljeni koridor kalorija: od mirovanja lakše do 3x mirovanja teže težine (trenutne ili ciljne).
        floor_kcal, ceiling_kcal = self.agent.calculations.kcal_bounds(reference_weight_kg, breed_midpoint_kg)
        self.agent.diagnostics["bounds_weight_kg"] = reference_weight_kg
        self.agent.context.update(floor=floor_kcal, ceiling=ceiling_kcal)
        self.agent.diagnostics.update(floor_kcal=floor_kcal, ceiling_kcal=ceiling_kcal)

        return None

    def _classify(self, previous_bcs: Optional[int] = None) -> Classification:
        measurement = self.agent.measurement

        bcs_deviation = self.agent.calculations.bcs_direction(measurement.bcs)
        weight_deviation = (-1 if measurement.weight < measurement.min_weight
                            else (1 if measurement.weight > measurement.max_weight else 0))
        breed_midpoint_kg = (measurement.min_weight + measurement.max_weight) / 2.0

        # BCS i težina govore suprotno (npr. BCS 9, a težina ispod minimuma pasmine).
        if bcs_deviation * weight_deviation == -1:
            return Classification(BodyState.IDEAL, False, True, measurement.weight, bcs_deviation, weight_deviation)

        bcs_unconfirmed = False
        if bcs_deviation == weight_deviation:
            effective_deviation = bcs_deviation
        elif (previous_bcs is not None
              and self.agent.calculations.bcs_direction(previous_bcs) == bcs_deviation):
            # BCS se ne slaže s težinom, ali ga je potvrdilo i prethodno mjerenje.
            effective_deviation = bcs_deviation
        else:
            effective_deviation = weight_deviation
            bcs_unconfirmed = True

        if effective_deviation == 1:
            target_weight_kg = breed_midpoint_kg if weight_deviation == 1 else measurement.min_weight
            return Classification(BodyState.OVERWEIGHT, weight_deviation != 1, False, target_weight_kg,
                                  bcs_deviation, weight_deviation, bcs_unconfirmed)
        if effective_deviation == -1:
            target_weight_kg = breed_midpoint_kg if weight_deviation == -1 else measurement.max_weight
            return Classification(BodyState.UNDERWEIGHT, weight_deviation != -1, False, target_weight_kg,
                                  bcs_deviation, weight_deviation, bcs_unconfirmed)

        return Classification(BodyState.IDEAL, weight_deviation != 0, False, measurement.weight,
                              bcs_deviation, weight_deviation, bcs_unconfirmed)
