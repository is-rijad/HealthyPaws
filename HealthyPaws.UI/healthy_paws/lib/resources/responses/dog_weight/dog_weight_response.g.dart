// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_weight_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogWeightResponse _$DogWeightResponseFromJson(Map<String, dynamic> json) =>
    DogWeightResponse(
      id: (json['id'] as num).toInt(),
      bcs: (json['bcs'] as num).toInt(),
      weight: (json['weight'] as num).toDouble(),
      calorie: (json['calorie'] as num).toDouble(),
      createdAt: DateTime.parse(json['createdAt'] as String),
      isRejected: json['isRejected'] as bool? ?? false,
      agent: json['agent'] == null
          ? null
          : AgentDecisionResponse.fromJson(
              json['agent'] as Map<String, dynamic>,
            ),
    );

Map<String, dynamic> _$DogWeightResponseToJson(DogWeightResponse instance) =>
    <String, dynamic>{
      'id': instance.id,
      'bcs': instance.bcs,
      'weight': instance.weight,
      'calorie': instance.calorie,
      'createdAt': instance.createdAt.toIso8601String(),
      'isRejected': instance.isRejected,
      'agent': instance.agent?.toJson(),
    };
