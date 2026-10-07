import 'package:healthy_paws/common/resources/json_parsable.dart'
    show JsonRequest;
import 'package:json_annotation/json_annotation.dart';

part 'request_reset_password_request.g.dart';

@JsonSerializable()
class RequestResetPasswordRequest implements JsonRequest {
  String email;

  RequestResetPasswordRequest({required this.email});

  @override
  Map<String, dynamic> toJson() => _$RequestResetPasswordRequestToJson(this);

  factory RequestResetPasswordRequest.fromJson(Map<String, dynamic> json) =>
      _$RequestResetPasswordRequestFromJson(json);
}
