import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'dog_weight_insert_request.g.dart';

@JsonSerializable()
class DogWeightInsertRequest extends JsonRequest {
  int bcs;
  double weight;
  double calorie;
  DateTime? createdAt;
  DogWeightInsertRequest({
    required this.bcs,
    required this.weight,
    required this.calorie,
    this.createdAt,
  });

  @override
  Map<String, dynamic> toJson() => _$DogWeightInsertRequestToJson(this);

  factory DogWeightInsertRequest.fromJson(Map<String, dynamic> json) =>
      _$DogWeightInsertRequestFromJson(json);
}
