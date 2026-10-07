import 'package:healthy_paws/common/resources/json_parsable.dart';

class AccessTokenPayload implements JsonParsable {
  String firstName;
  String lastName;
  int id;

  AccessTokenPayload({
    required this.firstName,
    required this.lastName,
    required this.id,
  });

  @override
  Map<String, dynamic> toJson() => {
    "name": firstName,
    "family_name": lastName,
    "sub": id,
  };

  factory AccessTokenPayload.fromJson(Map<String, dynamic> json) =>
      AccessTokenPayload(
        firstName: json["name"],
        lastName: json["family_name"],
        id: int.parse(json["sub"]),
      );
}
