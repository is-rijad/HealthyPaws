import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/responses/breed/breed_response.dart';
import 'package:healthy_paws/resources/search_objects/breed/breed_search_object.dart';

class BreedsProvider extends BaseGetProvider<BreedResponse, BreedSearchObject> {
  new() : super(providerName: "breeds");
}
