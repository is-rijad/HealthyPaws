import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';
import 'package:json_annotation/json_annotation.dart';

part 'agent_alert_search_object.g.dart';

@JsonSerializable()
class AgentAlertSearchObject extends BaseSearchObject {
  int? dogId;
  bool? isOpen;
  AgentAlertSearchObject({
    required super.page,
    required super.pageSize,
    this.dogId,
    this.isOpen,
  });

  @override
  Map<String, dynamic> toJson() => _$AgentAlertSearchObjectToJson(this);

  factory AgentAlertSearchObject.fromJson(Map<String, dynamic> json) =>
      _$AgentAlertSearchObjectFromJson(json);
}
