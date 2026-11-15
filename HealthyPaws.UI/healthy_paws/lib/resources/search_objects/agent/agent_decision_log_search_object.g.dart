// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'agent_decision_log_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AgentDecisionLogSearchObject _$AgentDecisionLogSearchObjectFromJson(
  Map<String, dynamic> json,
) => AgentDecisionLogSearchObject(
  page: (json['page'] as num).toInt(),
  pageSize: (json['pageSize'] as num).toInt(),
  dogId: (json['dogId'] as num?)?.toInt(),
);

Map<String, dynamic> _$AgentDecisionLogSearchObjectToJson(
  AgentDecisionLogSearchObject instance,
) => <String, dynamic>{
  'page': instance.page,
  'pageSize': instance.pageSize,
  'dogId': instance.dogId,
};
