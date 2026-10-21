import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'treat_response.g.dart';

@JsonSerializable()
class TreatResponse extends JsonResponse {
  @override
  int id;
  String name;
  double caloriesPerPiece;
  TreatResponse({
    required this.id,
    required this.name,
    required this.caloriesPerPiece,
  });

  @override
  Map<String, dynamic> toJson() => _$TreatResponseToJson(this);

  factory TreatResponse.fromJson(Map<String, dynamic> json) =>
      _$TreatResponseFromJson(json);
}
