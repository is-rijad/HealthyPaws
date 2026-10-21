import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/responses/dog_weight/dog_weight_response.dart';
import 'package:healthy_paws/resources/search_objects/dog_weight/dog_weight_search_object.dart';

class DogWeightsProvider
    extends BaseGetProvider<DogWeightResponse, DogWeightSearchObject> {
  new() : super(providerName: "dogweights");
}
