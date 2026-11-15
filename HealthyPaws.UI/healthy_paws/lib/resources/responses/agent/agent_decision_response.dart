import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'agent_decision_response.g.dart';

@JsonSerializable()
class AgentDecisionResponse extends JsonParsable {
  String action;
  bool planChanged;
  List<String> reasonCodes;
  String explanation;
  double? newDailyKcal;
  double? newDailyGrams;
  bool requiresHumanReview;
  DateTime nextEvaluationAt;
  AgentDecisionResponse({
    required this.action,
    required this.planChanged,
    required this.reasonCodes,
    required this.explanation,
    this.newDailyKcal,
    this.newDailyGrams,
    required this.requiresHumanReview,
    required this.nextEvaluationAt,
  });

  bool get isRemeasureRequested => action == "RequestRemeasure";

  @override
  Map<String, dynamic> toJson() => _$AgentDecisionResponseToJson(this);

  factory AgentDecisionResponse.fromJson(Map<String, dynamic> json) =>
      _$AgentDecisionResponseFromJson(json);
}
