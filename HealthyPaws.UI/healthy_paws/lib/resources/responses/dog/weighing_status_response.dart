import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:json_annotation/json_annotation.dart';

part 'weighing_status_response.g.dart';

@JsonSerializable()
class WeighingStatusResponse extends JsonParsable {
  int dogId;
  DateTime? nextMeasureAt;
  bool isDue;
  int daysOverdue;
  WeighingStatusResponse({
    required this.dogId,
    this.nextMeasureAt,
    required this.isDue,
    required this.daysOverdue,
  });

  @override
  Map<String, dynamic> toJson() => _$WeighingStatusResponseToJson(this);

  factory WeighingStatusResponse.fromJson(Map<String, dynamic> json) =>
      _$WeighingStatusResponseFromJson(json);
}
