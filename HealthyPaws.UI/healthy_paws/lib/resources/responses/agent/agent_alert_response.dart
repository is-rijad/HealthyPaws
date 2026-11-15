import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'agent_alert_response.g.dart';

@JsonSerializable()
class AgentAlertResponse extends JsonResponse {
  @override
  int id;
  int dogId;
  String reason;
  String explanation;
  DateTime firstSeenAt;
  DateTime lastSeenAt;
  int occurrences;
  DateTime? resolvedAt;
  String? resolvedBy;
  String category;
  String recommendedAction;
  AgentAlertResponse({
    required this.id,
    required this.dogId,
    required this.reason,
    required this.explanation,
    required this.firstSeenAt,
    required this.lastSeenAt,
    required this.occurrences,
    this.resolvedAt,
    this.resolvedBy,
    required this.category,
    required this.recommendedAction,
  });

  bool get isOpen => resolvedAt == null;

  @override
  Map<String, dynamic> toJson() => _$AgentAlertResponseToJson(this);

  factory AgentAlertResponse.fromJson(Map<String, dynamic> json) =>
      _$AgentAlertResponseFromJson(json);
}
