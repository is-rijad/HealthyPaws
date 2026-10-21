// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogResponse _$DogResponseFromJson(Map<String, dynamic> json) => DogResponse(
  id: (json['id'] as num).toInt(),
  breedId: (json['breedId'] as num).toInt(),
  name: json['name'] as String,
  dateOfBirth: DateTime.parse(json['dateOfBirth'] as String),
  gender: $enumDecode(_$DogGenderEnumMap, json['gender']),
  activityStatus: (json['activityStatus'] as num).toDouble(),
  isNeutered: json['isNeutered'] as bool,
  breed: json['breed'] == null
      ? null
      : BreedResponse.fromJson(json['breed'] as Map<String, dynamic>),
);

Map<String, dynamic> _$DogResponseToJson(DogResponse instance) =>
    <String, dynamic>{
      'id': instance.id,
      'breedId': instance.breedId,
      'name': instance.name,
      'dateOfBirth': instance.dateOfBirth.toIso8601String(),
      'gender': _$DogGenderEnumMap[instance.gender]!,
      'activityStatus': instance.activityStatus,
      'isNeutered': instance.isNeutered,
      'breed': instance.breed,
    };

const _$DogGenderEnumMap = {DogGender.male: 0, DogGender.female: 1};
