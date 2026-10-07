import 'package:healthy_paws/resources/search_objects/base_search_object/string_search_object.dart';
import 'package:json_annotation/json_annotation.dart';
part 'dog_search_object.g.dart';

@JsonSerializable()
class DogSearchObject extends StringSearchObject {
  DogSearchObject({
    required super.page,
    required super.pageSize,
    super.contains,
  });

  @override
  Map<String, dynamic> toJson() => _$DogSearchObjectToJson(this);

  factory DogSearchObject.fromJson(Map<String, dynamic> json) =>
      _$DogSearchObjectFromJson(json);
}
