import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/resources/responses/agent/agent_decision_log_response.dart';
import 'package:healthy_paws/resources/search_objects/agent/agent_decision_log_search_object.dart';

class AgentLogsProvider
    extends
        BaseGetProvider<
          AgentDecisionLogResponse,
          AgentDecisionLogSearchObject
        > {
  AgentLogsProvider() : super(providerName: "agentlogs");
}
