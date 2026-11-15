// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'agent_alert_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AgentAlertResponse _$AgentAlertResponseFromJson(Map<String, dynamic> json) =>
    AgentAlertResponse(
      id: (json['id'] as num).toInt(),
      dogId: (json['dogId'] as num).toInt(),
      reason: json['reason'] as String,
      explanation: json['explanation'] as String,
      firstSeenAt: DateTime.parse(json['firstSeenAt'] as String),
      lastSeenAt: DateTime.parse(json['lastSeenAt'] as String),
      occurrences: (json['occurrences'] as num).toInt(),
      resolvedAt: json['resolvedAt'] == null
          ? null
          : DateTime.parse(json['resolvedAt'] as String),
      resolvedBy: json['resolvedBy'] as String?,
      category: json['category'] as String,
      recommendedAction: json['recommendedAction'] as String,
    );

Map<String, dynamic> _$AgentAlertResponseToJson(AgentAlertResponse instance) =>
    <String, dynamic>{
      'id': instance.id,
      'dogId': instance.dogId,
      'reason': instance.reason,
      'explanation': instance.explanation,
      'firstSeenAt': instance.firstSeenAt.toIso8601String(),
      'lastSeenAt': instance.lastSeenAt.toIso8601String(),
      'occurrences': instance.occurrences,
      'resolvedAt': instance.resolvedAt?.toIso8601String(),
      'resolvedBy': instance.resolvedBy,
      'category': instance.category,
      'recommendedAction': instance.recommendedAction,
    };
