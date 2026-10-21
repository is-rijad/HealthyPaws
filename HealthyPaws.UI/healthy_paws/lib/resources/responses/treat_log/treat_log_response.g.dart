// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'treat_log_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TreatLogResponse _$TreatLogResponseFromJson(Map<String, dynamic> json) =>
    TreatLogResponse(
      id: (json['id'] as num).toInt(),
      dogId: (json['dogId'] as num).toInt(),
      treatId: (json['treatId'] as num?)?.toInt(),
      treatName: json['treatName'] as String,
      quantity: (json['quantity'] as num).toInt(),
      calories: (json['calories'] as num).toDouble(),
      givenAt: DateTime.parse(json['givenAt'] as String),
    );

Map<String, dynamic> _$TreatLogResponseToJson(TreatLogResponse instance) =>
    <String, dynamic>{
      'id': instance.id,
      'dogId': instance.dogId,
      'treatId': instance.treatId,
      'treatName': instance.treatName,
      'quantity': instance.quantity,
      'calories': instance.calories,
      'givenAt': instance.givenAt.toIso8601String(),
    };
