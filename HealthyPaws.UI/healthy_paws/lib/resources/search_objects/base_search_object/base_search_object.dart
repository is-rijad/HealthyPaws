import 'package:healthy_paws/common/resources/json_parsable.dart';

abstract class BaseSearchObject extends JsonParsable {
  int page;
  final int pageSize;

  BaseSearchObject({required this.page, required this.pageSize});
}
