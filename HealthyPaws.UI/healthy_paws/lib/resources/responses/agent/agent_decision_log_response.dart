import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'agent_decision_log_response.g.dart';

@JsonSerializable()
class AgentDecisionLogResponse extends JsonResponse {
  @override
  int id;
  int dogId;
  DateTime measuredAt;
  String action;
  String outcome;
  String reasonCodes;
  String explanation;
  double? newDailyKcal;
  double? newDailyGrams;
  bool isRejected;
  bool requiresHumanReview;
  DateTime receivedAt;
  AgentDecisionLogResponse({
    required this.id,
    required this.dogId,
    required this.measuredAt,
    required this.action,
    required this.outcome,
    required this.reasonCodes,
    required this.explanation,
    this.newDailyKcal,
    this.newDailyGrams,
    required this.isRejected,
    required this.requiresHumanReview,
    required this.receivedAt,
  });

  @override
  Map<String, dynamic> toJson() => _$AgentDecisionLogResponseToJson(this);

  factory AgentDecisionLogResponse.fromJson(Map<String, dynamic> json) =>
      _$AgentDecisionLogResponseFromJson(json);
}
