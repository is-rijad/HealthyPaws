import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/requests/treat/treat_insert_request.dart';
import 'package:healthy_paws/resources/responses/treat/treat_response.dart';
import 'package:healthy_paws/resources/search_objects/treat/treat_search_object.dart';

class TreatsProvider
    extends
        BaseProvider<
          TreatResponse,
          TreatSearchObject,
          TreatInsertRequest,
          TreatInsertRequest
        > {
  new() : super(providerName: "treats");
}
