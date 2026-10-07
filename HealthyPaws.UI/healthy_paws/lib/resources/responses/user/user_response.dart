import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'user_response.g.dart';

@JsonSerializable()
class UserResponse extends JsonResponse {
  @override
  int id;
  String firstName;
  String lastName;
  String email;

  UserResponse({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
  });

  @override
  Map<String, dynamic> toJson() => _$UserResponseToJson(this);

  factory UserResponse.fromJson(Map<String, dynamic> json) =>
      _$UserResponseFromJson(json);
}
