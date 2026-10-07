import 'package:json_annotation/json_annotation.dart';

enum DogGender {
  @JsonValue(0)
  male,
  @JsonValue(1)
  female,
}

final dogGenderMap = {DogGender.male: "Muško", DogGender.female: "Žensko"};
