import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/requests/device/device_insert_request.dart';
import 'package:healthy_paws/resources/responses/device/device_response.dart';
import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';

class DevicesProvider
    extends
        BaseProvider<
          DeviceResponse,
          BaseSearchObject,
          DeviceInsertRequest,
          DeviceInsertRequest
        > {
  new() : super(providerName: "devices");
}
