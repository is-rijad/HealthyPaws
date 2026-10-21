import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';
import 'package:json_annotation/json_annotation.dart';

part 'treat_log_search_object.g.dart';

@JsonSerializable()
class TreatLogSearchObject extends BaseSearchObject {
  int? dogId;
  DateTime? from;
  DateTime? to;
  TreatLogSearchObject({
    required super.page,
    required super.pageSize,
    this.dogId,
    this.from,
    this.to,
  });

  @override
  Map<String, dynamic> toJson() => _$TreatLogSearchObjectToJson(this);

  factory TreatLogSearchObject.fromJson(Map<String, dynamic> json) =>
      _$TreatLogSearchObjectFromJson(json);
}
