import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';

part 'dog_insert_request.g.dart';

@JsonSerializable()
class DogInsertRequest extends JsonRequest {
  int breedId;
  String name;
  DateTime dateOfBirth;
  DogGender gender;
  double activityStatus;
  bool isNeutered;
  DogInsertRequest({
    required this.breedId,
    required this.name,
    required this.dateOfBirth,
    required this.gender,
    required this.activityStatus,
    required this.isNeutered,
  });

  @override
  Map<String, dynamic> toJson() => _$DogInsertRequestToJson(this);

  factory DogInsertRequest.fromJson(Map<String, dynamic> json) =>
      _$DogInsertRequestFromJson(json);
}
