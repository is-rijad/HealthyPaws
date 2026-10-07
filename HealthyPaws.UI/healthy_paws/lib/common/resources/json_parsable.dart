abstract class JsonParsable {
  Map<String, dynamic> toJson();
}

abstract class JsonRequest extends JsonParsable {}

abstract class JsonResponse extends JsonParsable {
  abstract final int id;
}
