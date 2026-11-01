import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_insert_request.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_update_request.dart';
import 'package:healthy_paws/resources/responses/dog_weight/dog_weight_response.dart';
import 'package:healthy_paws/resources/search_objects/dog_weight/dog_weight_search_object.dart';

class DogWeightsProvider
    extends
        BaseProvider<
          DogWeightResponse,
          DogWeightSearchObject,
          DogWeightInsertRequest,
          DogWeightUpdateRequest
        > {
  new() : super(providerName: "dogweights");
}
