import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'treat_insert_request.g.dart';

@JsonSerializable()
class TreatInsertRequest extends JsonRequest {
  String name;
  double caloriesPerPiece;
  TreatInsertRequest({required this.name, required this.caloriesPerPiece});

  @override
  Map<String, dynamic> toJson() => _$TreatInsertRequestToJson(this);

  factory TreatInsertRequest.fromJson(Map<String, dynamic> json) =>
      _$TreatInsertRequestFromJson(json);
}
