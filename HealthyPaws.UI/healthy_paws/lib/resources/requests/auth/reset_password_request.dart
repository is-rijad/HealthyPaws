import 'package:healthy_paws/resources/requests/auth/request_reset_password_request.dart';
import 'package:json_annotation/json_annotation.dart';

part 'reset_password_request.g.dart';

@JsonSerializable()
class ResetPasswordRequest extends RequestResetPasswordRequest {
  String code;
  String password;
  String passwordAgain;

  ResetPasswordRequest({
    required super.email,
    required this.code,
    required this.password,
    required this.passwordAgain,
  });

  @override
  Map<String, dynamic> toJson() => _$ResetPasswordRequestToJson(this);

  factory ResetPasswordRequest.fromJson(Map<String, dynamic> json) =>
      _$ResetPasswordRequestFromJson(json);
}
