// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'treat_insert_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TreatInsertRequest _$TreatInsertRequestFromJson(Map<String, dynamic> json) =>
    TreatInsertRequest(
      name: json['name'] as String,
      caloriesPerPiece: (json['caloriesPerPiece'] as num).toDouble(),
    );

Map<String, dynamic> _$TreatInsertRequestToJson(TreatInsertRequest instance) =>
    <String, dynamic>{
      'name': instance.name,
      'caloriesPerPiece': instance.caloriesPerPiece,
    };
