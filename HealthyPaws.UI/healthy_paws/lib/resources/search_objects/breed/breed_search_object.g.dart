// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'breed_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

BreedSearchObject _$BreedSearchObjectFromJson(Map<String, dynamic> json) =>
    BreedSearchObject(
      page: (json['page'] as num).toInt(),
      pageSize: (json['pageSize'] as num).toInt(),
      contains: json['contains'] as String?,
    );

Map<String, dynamic> _$BreedSearchObjectToJson(BreedSearchObject instance) =>
    <String, dynamic>{
      'page': instance.page,
      'pageSize': instance.pageSize,
      'contains': instance.contains,
    };
