// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'device_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DeviceResponse _$DeviceResponseFromJson(Map<String, dynamic> json) =>
    DeviceResponse(
      id: (json['id'] as num).toInt(),
      dogId: (json['dogId'] as num).toInt(),
      currentWeight: (json['currentWeight'] as num).toDouble(),
      isConnected: json['isConnected'] as bool,
      dog: json['dog'] == null
          ? null
          : DogResponse.fromJson(json['dog'] as Map<String, dynamic>),
    );

Map<String, dynamic> _$DeviceResponseToJson(DeviceResponse instance) =>
    <String, dynamic>{
      'id': instance.id,
      'dogId': instance.dogId,
      'currentWeight': instance.currentWeight,
      'isConnected': instance.isConnected,
      'dog': instance.dog,
    };
