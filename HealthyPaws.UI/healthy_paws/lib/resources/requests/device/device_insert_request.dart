import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'device_insert_request.g.dart';

@JsonSerializable()
class DeviceInsertRequest extends JsonRequest {
  int dogId;
  DeviceInsertRequest({required this.dogId});

  @override
  Map<String, dynamic> toJson() => _$DeviceInsertRequestToJson(this);

  factory DeviceInsertRequest.fromJson(Map<String, dynamic> json) =>
      _$DeviceInsertRequestFromJson(json);
}
