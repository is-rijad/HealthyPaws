// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'treat_log_insert_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TreatLogInsertRequest _$TreatLogInsertRequestFromJson(
  Map<String, dynamic> json,
) => TreatLogInsertRequest(
  dogId: (json['dogId'] as num).toInt(),
  treatId: (json['treatId'] as num).toInt(),
  quantity: (json['quantity'] as num).toInt(),
  givenAt: json['givenAt'] == null
      ? null
      : DateTime.parse(json['givenAt'] as String),
);

Map<String, dynamic> _$TreatLogInsertRequestToJson(
  TreatLogInsertRequest instance,
) => <String, dynamic>{
  'dogId': instance.dogId,
  'treatId': instance.treatId,
  'quantity': instance.quantity,
  'givenAt': instance.givenAt?.toIso8601String(),
};
