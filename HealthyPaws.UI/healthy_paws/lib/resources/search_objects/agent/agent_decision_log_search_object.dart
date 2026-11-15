import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';
import 'package:json_annotation/json_annotation.dart';

part 'agent_decision_log_search_object.g.dart';

@JsonSerializable()
class AgentDecisionLogSearchObject extends BaseSearchObject {
  int? dogId;
  AgentDecisionLogSearchObject({
    required super.page,
    required super.pageSize,
    this.dogId,
  });

  @override
  Map<String, dynamic> toJson() => _$AgentDecisionLogSearchObjectToJson(this);

  factory AgentDecisionLogSearchObject.fromJson(Map<String, dynamic> json) =>
      _$AgentDecisionLogSearchObjectFromJson(json);
}
