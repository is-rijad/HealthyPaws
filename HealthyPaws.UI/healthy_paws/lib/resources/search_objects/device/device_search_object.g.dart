// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'device_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DeviceSearchObject _$DeviceSearchObjectFromJson(Map<String, dynamic> json) =>
    DeviceSearchObject(
      page: (json['page'] as num).toInt(),
      pageSize: (json['pageSize'] as num).toInt(),
    );

Map<String, dynamic> _$DeviceSearchObjectToJson(DeviceSearchObject instance) =>
    <String, dynamic>{'page': instance.page, 'pageSize': instance.pageSize};
