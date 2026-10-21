import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';
import 'package:json_annotation/json_annotation.dart';

part 'dog_weight_search_object.g.dart';

@JsonSerializable(includeIfNull: false)
class DogWeightSearchObject extends BaseSearchObject {
  int? dogId;
  DateTime? from;
  DateTime? to;
  double? minWeight;
  double? maxWeight;
  int? bcs;
  String? orderBy;
  bool descending;
  DogWeightSearchObject({
    required super.page,
    required super.pageSize,
    this.dogId,
    this.from,
    this.to,
    this.minWeight,
    this.maxWeight,
    this.bcs,
    this.orderBy,
    this.descending = true,
  });

  @override
  Map<String, dynamic> toJson() => _$DogWeightSearchObjectToJson(this);

  factory DogWeightSearchObject.fromJson(Map<String, dynamic> json) =>
      _$DogWeightSearchObjectFromJson(json);
}
