import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'device_update_request.g.dart';

@JsonSerializable()
class DeviceUpdateRequest extends JsonRequest {
  int dogId;
  DeviceUpdateRequest({required this.dogId});

  @override
  Map<String, dynamic> toJson() => _$DeviceUpdateRequestToJson(this);

  factory DeviceUpdateRequest.fromJson(Map<String, dynamic> json) =>
      _$DeviceUpdateRequestFromJson(json);
}
