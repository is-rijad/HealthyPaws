import traceback

from calorie_agent.calorie_agent import CalorieAgent
from calorie_agent.config.config import DEFAULT_AGENT_CONFIG
from .calorie_agent_pb2_grpc import CalorieAgentServicer


class CalorieAgentService(CalorieAgentServicer):
    def Tick(self, request, context):
        try:
            return CalorieAgent(DEFAULT_AGENT_CONFIG).run(request)
        except Exception:
            print(traceback.format_exc())
