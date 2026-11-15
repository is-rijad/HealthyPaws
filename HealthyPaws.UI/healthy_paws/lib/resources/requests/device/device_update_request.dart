import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'device_update_request.g.dart';

/// Uređivanje uređaja nije podržano; tip postoji samo zbog generičkog providera.
@JsonSerializable()
class DeviceUpdateRequest extends JsonRequest {
  DeviceUpdateRequest();

  @override
  Map<String, dynamic> toJson() => _$DeviceUpdateRequestToJson(this);

  factory DeviceUpdateRequest.fromJson(Map<String, dynamic> json) =>
      _$DeviceUpdateRequestFromJson(json);
}
