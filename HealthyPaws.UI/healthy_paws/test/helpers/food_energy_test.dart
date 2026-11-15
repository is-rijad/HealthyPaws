import 'package:flutter_test/flutter_test.dart';
import 'package:healthy_paws/common/utils/food_energy.dart';

void main() {
  test('Atwater example from label', () {
    final r = FoodEnergy.estimate(
        protein: 26, fat: 14, fiber: 3, moisture: 10, ash: 7)!;
    expect(r.nfe, 40);
    expect(r.kcalPer100g, closeTo(350, 1e-9));
    expect(r.ashEstimated, isFalse);
  });

  test('missing ash is estimated by food type', () {
    final dry = FoodEnergy.estimate(protein: 26, fat: 14, fiber: 3, moisture: 10)!;
    expect(dry.ash, 7);
    expect(dry.ashEstimated, isTrue);
    final wet = FoodEnergy.estimate(protein: 8, fat: 5, fiber: 1, moisture: 80)!;
    expect(wet.ash, 2.5);
  });

  test('invalid composition returns null', () {
    expect(FoodEnergy.estimate(protein: 60, fat: 30, fiber: 10, moisture: 10), isNull);
    expect(FoodEnergy.estimate(protein: -1, fat: 5, fiber: 1, moisture: 10), isNull);
  });
}
