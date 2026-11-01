import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'dog_weight_update_request.g.dart';

@JsonSerializable()
class DogWeightUpdateRequest extends JsonRequest {
  int? bcs;
  double? weight;
  double? calorie;
  DogWeightUpdateRequest({this.bcs, this.weight, this.calorie});

  @override
  Map<String, dynamic> toJson() => _$DogWeightUpdateRequestToJson(this);

  factory DogWeightUpdateRequest.fromJson(Map<String, dynamic> json) =>
      _$DogWeightUpdateRequestFromJson(json);
}
