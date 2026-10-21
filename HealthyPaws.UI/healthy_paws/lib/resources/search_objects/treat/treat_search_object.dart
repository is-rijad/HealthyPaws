import 'package:healthy_paws/resources/search_objects/base_search_object/string_search_object.dart';
import 'package:json_annotation/json_annotation.dart';

part 'treat_search_object.g.dart';

@JsonSerializable()
class TreatSearchObject extends StringSearchObject {
  TreatSearchObject({
    required super.page,
    required super.pageSize,
    super.contains,
  });

  @override
  Map<String, dynamic> toJson() => _$TreatSearchObjectToJson(this);

  factory TreatSearchObject.fromJson(Map<String, dynamic> json) =>
      _$TreatSearchObjectFromJson(json);
}
