import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'treat_log_insert_request.g.dart';

@JsonSerializable()
class TreatLogInsertRequest extends JsonRequest {
  int dogId;
  int treatId;
  int quantity;
  DateTime? givenAt;
  TreatLogInsertRequest({
    required this.dogId,
    required this.treatId,
    required this.quantity,
    this.givenAt,
  });

  @override
  Map<String, dynamic> toJson() => _$TreatLogInsertRequestToJson(this);

  factory TreatLogInsertRequest.fromJson(Map<String, dynamic> json) =>
      _$TreatLogInsertRequestFromJson(json);
}
