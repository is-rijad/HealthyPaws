import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/requests/user/user_insert_request.dart';
import 'package:healthy_paws/resources/requests/user/user_update_request.dart';
import 'package:healthy_paws/resources/responses/user/user_response.dart';
import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';

class UsersProvider
    extends
        BaseProvider<
          UserResponse,
          BaseSearchObject,
          UserInsertRequest,
          UserUpdateRequest
        > {
  UsersProvider() : super(providerName: "users");
}
