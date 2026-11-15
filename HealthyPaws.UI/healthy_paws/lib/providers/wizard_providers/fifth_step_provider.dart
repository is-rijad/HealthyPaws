import 'package:healthy_paws/common/utils/food_energy.dart';
import 'package:material_ui/material_ui.dart';

class FifthStepProvider with ChangeNotifier {
  /// Kalorije hrane koju pas koristi (kcal/100g).
  double? calorie;

  /// Da li je vrijednost sa etikete ili procijenjena iz sastava.
  FoodCalorieSource? source;
}
