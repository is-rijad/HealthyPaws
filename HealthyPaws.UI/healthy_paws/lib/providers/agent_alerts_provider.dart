import 'package:healthy_paws/common/dio_client.dart';
import 'package:healthy_paws/common/env.dart';
import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/common/utils/json_parser.dart';
import 'package:healthy_paws/resources/responses/agent/agent_alert_response.dart';
import 'package:healthy_paws/resources/search_objects/agent/agent_alert_search_object.dart';

class AgentAlertsProvider
    extends BaseGetProvider<AgentAlertResponse, AgentAlertSearchObject> {
  AgentAlertsProvider() : super(providerName: "alerts");

  Future<AgentAlertResponse> resolve(int id) async {
    final response = await DioClient.dio.put(
      "${Environment.apiUrl}$providerName/$id/resolve",
    );
    return parseJson<AgentAlertResponse>(response.data);
  }
}
