// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'weighing_status_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

WeighingStatusResponse _$WeighingStatusResponseFromJson(
  Map<String, dynamic> json,
) => WeighingStatusResponse(
  dogId: (json['dogId'] as num).toInt(),
  nextMeasureAt: json['nextMeasureAt'] == null
      ? null
      : DateTime.parse(json['nextMeasureAt'] as String),
  isDue: json['isDue'] as bool,
  daysOverdue: (json['daysOverdue'] as num).toInt(),
);

Map<String, dynamic> _$WeighingStatusResponseToJson(
  WeighingStatusResponse instance,
) => <String, dynamic>{
  'dogId': instance.dogId,
  'nextMeasureAt': instance.nextMeasureAt?.toIso8601String(),
  'isDue': instance.isDue,
  'daysOverdue': instance.daysOverdue,
};
