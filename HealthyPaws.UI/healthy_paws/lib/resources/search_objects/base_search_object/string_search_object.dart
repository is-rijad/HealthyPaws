import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';

abstract class StringSearchObject extends BaseSearchObject {
  String? contains;
  StringSearchObject({
    required super.page,
    required super.pageSize,
    this.contains,
  });
}
