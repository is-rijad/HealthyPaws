// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_weight_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogWeightResponse _$DogWeightResponseFromJson(Map<String, dynamic> json) =>
    DogWeightResponse(
      bcs: (json['bcs'] as num).toInt(),
      weight: (json['weight'] as num).toDouble(),
      calorie: (json['calorie'] as num).toDouble(),
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$DogWeightResponseToJson(DogWeightResponse instance) =>
    <String, dynamic>{
      'bcs': instance.bcs,
      'weight': instance.weight,
      'calorie': instance.calorie,
      'createdAt': instance.createdAt.toIso8601String(),
    };
