import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'refresh_token_request.g.dart';

@JsonSerializable()
class RefreshTokenRequest implements JsonRequest {
  String accessToken;
  String refreshToken;

  RefreshTokenRequest({required this.accessToken, required this.refreshToken});

  factory RefreshTokenRequest.fromJson(Map<String, dynamic> json) =>
      _$RefreshTokenRequestFromJson(json);

  @override
  Map<String, dynamic> toJson() => _$RefreshTokenRequestToJson(this);
}
