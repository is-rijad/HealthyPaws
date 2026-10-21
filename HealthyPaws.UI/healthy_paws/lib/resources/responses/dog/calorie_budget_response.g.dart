// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'calorie_budget_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CalorieBudgetResponse _$CalorieBudgetResponseFromJson(
  Map<String, dynamic> json,
) => CalorieBudgetResponse(
  dogId: (json['dogId'] as num).toInt(),
  dailyBudget: (json['dailyBudget'] as num).toDouble(),
  treatCalories: (json['treatCalories'] as num).toDouble(),
  remaining: (json['remaining'] as num).toDouble(),
);

Map<String, dynamic> _$CalorieBudgetResponseToJson(
  CalorieBudgetResponse instance,
) => <String, dynamic>{
  'dogId': instance.dogId,
  'dailyBudget': instance.dailyBudget,
  'treatCalories': instance.treatCalories,
  'remaining': instance.remaining,
};
