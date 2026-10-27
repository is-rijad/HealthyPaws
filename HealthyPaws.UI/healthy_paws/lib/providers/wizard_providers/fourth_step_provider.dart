import 'package:material_ui/material_ui.dart';

class FourthStepProvider with ChangeNotifier {
  /// Okvirna vremena obroka u formatu "HH:mm:ss".
  List<String>? mealTimes;

  /// Kalorije hrane koju pas koristi (kcal/100g).
  double? calorie;
}
