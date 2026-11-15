// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'agent_decision_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AgentDecisionResponse _$AgentDecisionResponseFromJson(
  Map<String, dynamic> json,
) => AgentDecisionResponse(
  action: json['action'] as String,
  planChanged: json['planChanged'] as bool,
  reasonCodes: (json['reasonCodes'] as List<dynamic>)
      .map((e) => e as String)
      .toList(),
  explanation: json['explanation'] as String,
  newDailyKcal: (json['newDailyKcal'] as num?)?.toDouble(),
  newDailyGrams: (json['newDailyGrams'] as num?)?.toDouble(),
  requiresHumanReview: json['requiresHumanReview'] as bool,
  nextEvaluationAt: DateTime.parse(json['nextEvaluationAt'] as String),
);

Map<String, dynamic> _$AgentDecisionResponseToJson(
  AgentDecisionResponse instance,
) => <String, dynamic>{
  'action': instance.action,
  'planChanged': instance.planChanged,
  'reasonCodes': instance.reasonCodes,
  'explanation': instance.explanation,
  'newDailyKcal': instance.newDailyKcal,
  'newDailyGrams': instance.newDailyGrams,
  'requiresHumanReview': instance.requiresHumanReview,
  'nextEvaluationAt': instance.nextEvaluationAt.toIso8601String(),
};
