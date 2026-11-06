import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'calorie_budget_response.g.dart';

@JsonSerializable()
class CalorieBudgetResponse extends JsonParsable {
  int dogId;
  double dailyBudget;
  double treatCalories;
  double foodCalories;
  double remaining;
  CalorieBudgetResponse({
    required this.dogId,
    required this.dailyBudget,
    required this.treatCalories,
    required this.foodCalories,
    required this.remaining,
  });

  @override
  Map<String, dynamic> toJson() => _$CalorieBudgetResponseToJson(this);

  factory CalorieBudgetResponse.fromJson(Map<String, dynamic> json) =>
      _$CalorieBudgetResponseFromJson(json);
}
