import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_insert_request.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/search_objects/dog/dog_search_object.dart';

class DogsProvider
    extends
        BaseProvider<
          DogResponse,
          DogSearchObject,
          DogInsertRequest,
          DogInsertRequest
        > {
  new() : super(providerName: "dogs");
}
