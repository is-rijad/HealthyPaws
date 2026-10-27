import 'package:healthy_paws/common/resources/json_parsable.dart';

class AccessTokenPayload implements JsonParsable {
  int id;

  AccessTokenPayload({required this.id});

  @override
  Map<String, dynamic> toJson() => {"sub": id};

  factory AccessTokenPayload.fromJson(Map<String, dynamic> json) =>
      AccessTokenPayload(id: int.parse(json["sub"]));
}
