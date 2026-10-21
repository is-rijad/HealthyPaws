import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'treat_log_response.g.dart';

@JsonSerializable()
class TreatLogResponse extends JsonResponse {
  @override
  int id;
  int dogId;
  int? treatId;
  String treatName;
  int quantity;
  double calories;
  DateTime givenAt;
  TreatLogResponse({
    required this.id,
    required this.dogId,
    this.treatId,
    required this.treatName,
    required this.quantity,
    required this.calories,
    required this.givenAt,
  });

  @override
  Map<String, dynamic> toJson() => _$TreatLogResponseToJson(this);

  factory TreatLogResponse.fromJson(Map<String, dynamic> json) =>
      _$TreatLogResponseFromJson(json);
}
