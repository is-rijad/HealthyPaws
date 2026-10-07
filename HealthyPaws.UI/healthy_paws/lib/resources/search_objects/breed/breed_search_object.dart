import 'package:healthy_paws/resources/search_objects/base_search_object/string_search_object.dart';
import 'package:json_annotation/json_annotation.dart';
part 'breed_search_object.g.dart';

@JsonSerializable()
class BreedSearchObject extends StringSearchObject {
  BreedSearchObject({
    required super.page,
    required super.pageSize,
    super.contains,
  });

  @override
  Map<String, dynamic> toJson() => _$BreedSearchObjectToJson(this);

  factory BreedSearchObject.fromJson(Map<String, dynamic> json) =>
      _$BreedSearchObjectFromJson(json);
}
