// ignore_for_file: public_member_api_docs, sort_constructors_first
import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'breed_response.g.dart';

@JsonSerializable()
class BreedResponse extends JsonResponse {
  @override
  int id;
  String name;
  BreedResponse({
    required this.id,
    required this.name,
  });
  @override
  Map<String, dynamic> toJson() => _$BreedResponseToJson(this);

  factory BreedResponse.fromJson(Map<String, dynamic> json) =>
      _$BreedResponseFromJson(json);
}
