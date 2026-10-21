// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'treat_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TreatSearchObject _$TreatSearchObjectFromJson(Map<String, dynamic> json) =>
    TreatSearchObject(
      page: (json['page'] as num).toInt(),
      pageSize: (json['pageSize'] as num).toInt(),
      contains: json['contains'] as String?,
    );

Map<String, dynamic> _$TreatSearchObjectToJson(TreatSearchObject instance) =>
    <String, dynamic>{
      'page': instance.page,
      'pageSize': instance.pageSize,
      'contains': instance.contains,
    };
