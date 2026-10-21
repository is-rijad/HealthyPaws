import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/requests/treat_log/treat_log_insert_request.dart';
import 'package:healthy_paws/resources/responses/treat_log/treat_log_response.dart';
import 'package:healthy_paws/resources/search_objects/treat_log/treat_log_search_object.dart';

class TreatLogsProvider
    extends
        BaseProvider<
          TreatLogResponse,
          TreatLogSearchObject,
          TreatLogInsertRequest,
          TreatLogInsertRequest
        > {
  new() : super(providerName: "treatlogs");
}
