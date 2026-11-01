// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_weight_update_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogWeightUpdateRequest _$DogWeightUpdateRequestFromJson(
  Map<String, dynamic> json,
) => DogWeightUpdateRequest(
  bcs: (json['bcs'] as num?)?.toInt(),
  weight: (json['weight'] as num?)?.toDouble(),
  calorie: (json['calorie'] as num?)?.toDouble(),
);

Map<String, dynamic> _$DogWeightUpdateRequestToJson(
  DogWeightUpdateRequest instance,
) => <String, dynamic>{
  'bcs': instance.bcs,
  'weight': instance.weight,
  'calorie': instance.calorie,
};
