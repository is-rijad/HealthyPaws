import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';
import 'package:json_annotation/json_annotation.dart';
part 'device_search_object.g.dart';

@JsonSerializable()
class DeviceSearchObject extends BaseSearchObject {
  DeviceSearchObject({required super.page, required super.pageSize});

  @override
  Map<String, dynamic> toJson() => _$DeviceSearchObjectToJson(this);

  factory DeviceSearchObject.fromJson(Map<String, dynamic> json) =>
      _$DeviceSearchObjectFromJson(json);
}
