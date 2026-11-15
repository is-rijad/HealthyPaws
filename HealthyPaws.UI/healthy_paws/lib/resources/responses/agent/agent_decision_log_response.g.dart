// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'agent_decision_log_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AgentDecisionLogResponse _$AgentDecisionLogResponseFromJson(
  Map<String, dynamic> json,
) => AgentDecisionLogResponse(
  id: (json['id'] as num).toInt(),
  dogId: (json['dogId'] as num).toInt(),
  measuredAt: DateTime.parse(json['measuredAt'] as String),
  action: json['action'] as String,
  outcome: json['outcome'] as String,
  reasonCodes: json['reasonCodes'] as String,
  explanation: json['explanation'] as String,
  newDailyKcal: (json['newDailyKcal'] as num?)?.toDouble(),
  newDailyGrams: (json['newDailyGrams'] as num?)?.toDouble(),
  isRejected: json['isRejected'] as bool,
  requiresHumanReview: json['requiresHumanReview'] as bool,
  receivedAt: DateTime.parse(json['receivedAt'] as String),
);

Map<String, dynamic> _$AgentDecisionLogResponseToJson(
  AgentDecisionLogResponse instance,
) => <String, dynamic>{
  'id': instance.id,
  'dogId': instance.dogId,
  'measuredAt': instance.measuredAt.toIso8601String(),
  'action': instance.action,
  'outcome': instance.outcome,
  'reasonCodes': instance.reasonCodes,
  'explanation': instance.explanation,
  'newDailyKcal': instance.newDailyKcal,
  'newDailyGrams': instance.newDailyGrams,
  'isRejected': instance.isRejected,
  'requiresHumanReview': instance.requiresHumanReview,
  'receivedAt': instance.receivedAt.toIso8601String(),
};
