// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'breed_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

BreedResponse _$BreedResponseFromJson(Map<String, dynamic> json) =>
    BreedResponse(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String,
    );

Map<String, dynamic> _$BreedResponseToJson(BreedResponse instance) =>
    <String, dynamic>{'id': instance.id, 'name': instance.name};
