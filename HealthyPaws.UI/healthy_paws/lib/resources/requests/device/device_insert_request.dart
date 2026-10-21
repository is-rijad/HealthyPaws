import 'package:healthy_paws/resources/requests/dog/dog_insert_request.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_insert_request.dart';
import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'device_insert_request.g.dart';

@JsonSerializable()
class DeviceInsertRequest extends JsonRequest {
  String macAddress;
  DogInsertRequest? dog;
  DogWeightInsertRequest? dogWeight;
  List<String>? mealTimes;
  DeviceInsertRequest({
    required this.macAddress,
    this.dog,
    this.dogWeight,
    this.mealTimes,
  });

  @override
  Map<String, dynamic> toJson() => _$DeviceInsertRequestToJson(this);

  factory DeviceInsertRequest.fromJson(Map<String, dynamic> json) =>
      _$DeviceInsertRequestFromJson(json);
}
