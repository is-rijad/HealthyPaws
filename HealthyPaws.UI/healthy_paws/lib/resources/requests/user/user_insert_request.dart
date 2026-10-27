import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'user_insert_request.g.dart';

@JsonSerializable()
class UserInsertRequest extends JsonRequest {
  String email;
  String password;
  String passwordAgain;

  UserInsertRequest({
    required this.email,
    required this.password,
    required this.passwordAgain,
  });

  @override
  Map<String, dynamic> toJson() => _$UserInsertRequestToJson(this);

  factory UserInsertRequest.fromJson(Map<String, dynamic> json) =>
      _$UserInsertRequestFromJson(json);
}
