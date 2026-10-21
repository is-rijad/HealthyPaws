// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'treat_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TreatResponse _$TreatResponseFromJson(Map<String, dynamic> json) =>
    TreatResponse(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String,
      caloriesPerPiece: (json['caloriesPerPiece'] as num).toDouble(),
    );

Map<String, dynamic> _$TreatResponseToJson(TreatResponse instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'caloriesPerPiece': instance.caloriesPerPiece,
    };
