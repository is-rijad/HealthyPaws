import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:healthy_paws/resources/responses/agent/agent_decision_response.dart';

part 'dog_weight_response.g.dart';

@JsonSerializable(explicitToJson: true)
class DogWeightResponse extends JsonResponse {
  @override
  int id;
  int bcs;
  double weight;
  double calorie;

  /// 0 = sa etikete, 1 = procijenjeno iz sastava.
  int calorieSource;
  DateTime createdAt;
  bool isRejected;
  AgentDecisionResponse? agent;
  DogWeightResponse({
    required this.id,
    required this.bcs,
    required this.weight,
    required this.calorie,
    this.calorieSource = 0,
    required this.createdAt,
    this.isRejected = false,
    this.agent,
  });
  @override
  Map<String, dynamic> toJson() => _$DogWeightResponseToJson(this);

  factory DogWeightResponse.fromJson(Map<String, dynamic> json) =>
      _$DogWeightResponseFromJson(json);
}
