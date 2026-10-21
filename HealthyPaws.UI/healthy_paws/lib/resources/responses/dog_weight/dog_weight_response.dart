import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'dog_weight_response.g.dart';

@JsonSerializable()
class DogWeightResponse extends JsonParsable {
  int bcs;
  double weight;
  double calorie;
  DateTime createdAt;
  DogWeightResponse({
    required this.bcs,
    required this.weight,
    required this.calorie,
    required this.createdAt,
  });
  @override
  Map<String, dynamic> toJson() => _$DogWeightResponseToJson(this);

  factory DogWeightResponse.fromJson(Map<String, dynamic> json) =>
      _$DogWeightResponseFromJson(json);
}
