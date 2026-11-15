// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_weight_insert_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogWeightInsertRequest _$DogWeightInsertRequestFromJson(
  Map<String, dynamic> json,
) => DogWeightInsertRequest(
  dogId: (json['dogId'] as num?)?.toInt(),
  bcs: (json['bcs'] as num).toInt(),
  weight: (json['weight'] as num).toDouble(),
  calorie: (json['calorie'] as num).toDouble(),
  calorieSource: (json['calorieSource'] as num?)?.toInt() ?? 0,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$DogWeightInsertRequestToJson(
  DogWeightInsertRequest instance,
) => <String, dynamic>{
  'dogId': instance.dogId,
  'bcs': instance.bcs,
  'weight': instance.weight,
  'calorie': instance.calorie,
  'calorieSource': instance.calorieSource,
  'createdAt': instance.createdAt?.toIso8601String(),
};
