// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_insert_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogInsertRequest _$DogInsertRequestFromJson(Map<String, dynamic> json) =>
    DogInsertRequest(
      breedId: (json['breedId'] as num).toInt(),
      name: json['name'] as String,
      dateOfBirth: DateTime.parse(json['dateOfBirth'] as String),
      gender: $enumDecode(_$DogGenderEnumMap, json['gender']),
      activityStatus: (json['activityStatus'] as num).toDouble(),
      bcs: (json['bcs'] as num).toInt(),
      isNeutered: json['isNeutered'] as bool,
    );

Map<String, dynamic> _$DogInsertRequestToJson(DogInsertRequest instance) =>
    <String, dynamic>{
      'breedId': instance.breedId,
      'name': instance.name,
      'dateOfBirth': instance.dateOfBirth.toIso8601String(),
      'gender': _$DogGenderEnumMap[instance.gender]!,
      'activityStatus': instance.activityStatus,
      'bcs': instance.bcs,
      'isNeutered': instance.isNeutered,
    };

const _$DogGenderEnumMap = {DogGender.male: 0, DogGender.female: 1};
