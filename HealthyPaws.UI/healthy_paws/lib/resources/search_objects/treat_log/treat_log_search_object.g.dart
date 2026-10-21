// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'treat_log_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TreatLogSearchObject _$TreatLogSearchObjectFromJson(
  Map<String, dynamic> json,
) => TreatLogSearchObject(
  page: (json['page'] as num).toInt(),
  pageSize: (json['pageSize'] as num).toInt(),
  dogId: (json['dogId'] as num?)?.toInt(),
  from: json['from'] == null ? null : DateTime.parse(json['from'] as String),
  to: json['to'] == null ? null : DateTime.parse(json['to'] as String),
);

Map<String, dynamic> _$TreatLogSearchObjectToJson(
  TreatLogSearchObject instance,
) => <String, dynamic>{
  'page': instance.page,
  'pageSize': instance.pageSize,
  'dogId': instance.dogId,
  'from': instance.from?.toIso8601String(),
  'to': instance.to?.toIso8601String(),
};
