// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'device_insert_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DeviceInsertRequest _$DeviceInsertRequestFromJson(Map<String, dynamic> json) =>
    DeviceInsertRequest(
      macAddress: json['macAddress'] as String,
      dog: json['dog'] == null
          ? null
          : DogInsertRequest.fromJson(json['dog'] as Map<String, dynamic>),
      dogWeight: json['dogWeight'] == null
          ? null
          : DogWeightInsertRequest.fromJson(
              json['dogWeight'] as Map<String, dynamic>,
            ),
    );

Map<String, dynamic> _$DeviceInsertRequestToJson(
  DeviceInsertRequest instance,
) => <String, dynamic>{
  'macAddress': instance.macAddress,
  'dog': instance.dog,
  'dogWeight': instance.dogWeight,
};
