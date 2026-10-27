// ignore_for_file: public_member_api_docs, sort_constructors_first
import 'package:json_annotation/json_annotation.dart';

import 'package:healthy_paws/common/resources/json_parsable.dart';

part 'user_update_request.g.dart';

@JsonSerializable()
class UserUpdateRequest extends JsonRequest {
  String email;
  bool changePassword;
  String? oldPassword;
  String? password;
  String? passwordAgain;

  UserUpdateRequest({
    required this.email,
    required this.changePassword,
    this.oldPassword,
    this.password,
    this.passwordAgain,
  });

  @override
  Map<String, dynamic> toJson() => _$UserUpdateRequestToJson(this);

  factory UserUpdateRequest.fromJson(Map<String, dynamic> json) =>
      _$UserUpdateRequestFromJson(json);
}
