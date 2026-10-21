// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'device_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DeviceResponse _$DeviceResponseFromJson(Map<String, dynamic> json) =>
    DeviceResponse(
      id: (json['id'] as num).toInt(),
      dogId: (json['dogId'] as num).toInt(),
      macAddress: json['macAddress'] as String,
      isConnected: json['isConnected'] as bool,
      dog: json['dog'] == null
          ? null
          : DogResponse.fromJson(json['dog'] as Map<String, dynamic>),
      lastWeightRecord: json['lastWeightRecord'] == null
          ? null
          : DogWeightResponse.fromJson(
              json['lastWeightRecord'] as Map<String, dynamic>,
            ),
    );

Map<String, dynamic> _$DeviceResponseToJson(DeviceResponse instance) =>
    <String, dynamic>{
      'id': instance.id,
      'dogId': instance.dogId,
      'macAddress': instance.macAddress,
      'isConnected': instance.isConnected,
      'dog': instance.dog,
      'lastWeightRecord': instance.lastWeightRecord,
    };
