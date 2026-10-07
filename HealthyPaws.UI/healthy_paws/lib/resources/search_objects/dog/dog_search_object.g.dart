// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogSearchObject _$DogSearchObjectFromJson(Map<String, dynamic> json) =>
    DogSearchObject(
      page: (json['page'] as num).toInt(),
      pageSize: (json['pageSize'] as num).toInt(),
      contains: json['contains'] as String?,
    );

Map<String, dynamic> _$DogSearchObjectToJson(DogSearchObject instance) =>
    <String, dynamic>{
      'page': instance.page,
      'pageSize': instance.pageSize,
      'contains': instance.contains,
    };
