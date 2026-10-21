import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';
import 'package:healthy_paws/resources/responses/breed/breed_response.dart';
import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'dog_response.g.dart';

@JsonSerializable()
class DogResponse extends JsonResponse {
  @override
  int id;
  int breedId;
  String name;
  DateTime dateOfBirth;
  DogGender gender;
  double activityStatus;
  bool isNeutered;
  BreedResponse? breed;
  DogResponse({
    required this.id,
    required this.breedId,
    required this.name,
    required this.dateOfBirth,
    required this.gender,
    required this.activityStatus,
    required this.isNeutered,
    this.breed,
  });
  @override
  Map<String, dynamic> toJson() => _$DogResponseToJson(this);

  factory DogResponse.fromJson(Map<String, dynamic> json) =>
      _$DogResponseFromJson(json);
}
