// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'agent_alert_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AgentAlertSearchObject _$AgentAlertSearchObjectFromJson(
  Map<String, dynamic> json,
) => AgentAlertSearchObject(
  page: (json['page'] as num).toInt(),
  pageSize: (json['pageSize'] as num).toInt(),
  dogId: (json['dogId'] as num?)?.toInt(),
  isOpen: json['isOpen'] as bool?,
);

Map<String, dynamic> _$AgentAlertSearchObjectToJson(
  AgentAlertSearchObject instance,
) => <String, dynamic>{
  'page': instance.page,
  'pageSize': instance.pageSize,
  'dogId': instance.dogId,
  'isOpen': instance.isOpen,
};
