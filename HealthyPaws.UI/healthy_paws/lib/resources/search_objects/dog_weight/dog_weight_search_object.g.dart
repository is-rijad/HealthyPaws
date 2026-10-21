// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dog_weight_search_object.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DogWeightSearchObject _$DogWeightSearchObjectFromJson(
  Map<String, dynamic> json,
) => DogWeightSearchObject(
  page: (json['page'] as num).toInt(),
  pageSize: (json['pageSize'] as num).toInt(),
  dogId: (json['dogId'] as num?)?.toInt(),
  from: json['from'] == null ? null : DateTime.parse(json['from'] as String),
  to: json['to'] == null ? null : DateTime.parse(json['to'] as String),
  minWeight: (json['minWeight'] as num?)?.toDouble(),
  maxWeight: (json['maxWeight'] as num?)?.toDouble(),
  bcs: (json['bcs'] as num?)?.toInt(),
  orderBy: json['orderBy'] as String?,
  descending: json['descending'] as bool? ?? true,
);

Map<String, dynamic> _$DogWeightSearchObjectToJson(
  DogWeightSearchObject instance,
) => <String, dynamic>{
  'page': instance.page,
  'pageSize': instance.pageSize,
  'dogId': ?instance.dogId,
  'from': ?instance.from?.toIso8601String(),
  'to': ?instance.to?.toIso8601String(),
  'minWeight': ?instance.minWeight,
  'maxWeight': ?instance.maxWeight,
  'bcs': ?instance.bcs,
  'orderBy': ?instance.orderBy,
  'descending': instance.descending,
};
