import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';

part 'device_response.g.dart';

@JsonSerializable()
class DeviceResponse extends JsonResponse {
  @override
  int id;
  int dogId;
  double currentWeight;
  bool isConnected;
  DogResponse? dog;
  DeviceResponse({
    required this.id,
    required this.dogId,
    required this.currentWeight,
    required this.isConnected,
    this.dog,
  });
  @override
  Map<String, dynamic> toJson() => _$DeviceResponseToJson(this);

  factory DeviceResponse.fromJson(Map<String, dynamic> json) =>
      _$DeviceResponseFromJson(json);
}
