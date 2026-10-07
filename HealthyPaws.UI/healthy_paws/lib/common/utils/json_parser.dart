import 'package:healthy_paws/common/resources/access_token_payload.dart';
import 'package:healthy_paws/resources/responses/auth/access_token_response.dart';
import 'package:healthy_paws/resources/responses/breed/breed_response.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/responses/user/user_response.dart';

typedef _FromJson<T> = T Function(Map<String, dynamic> json);

final Map<Type, _FromJson> _jsonFactories = {
  AccessTokenPayload: (json) => AccessTokenPayload.fromJson(json),
  AccessTokenResponse: (json) => AccessTokenResponse.fromJson(json),
  UserResponse: (json) => UserResponse.fromJson(json),
  DogResponse: (json) => DogResponse.fromJson(json),
  BreedResponse: (json) => BreedResponse.fromJson(json),
};

T parseJson<T>(Map<String, dynamic> json) {
  final fromJson = _jsonFactories[T];
  if (fromJson == null) throw Exception("No factory for type $T");
  return fromJson(json) as T;
}
