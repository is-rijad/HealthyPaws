from .calorie_agent_pb2_grpc import CalorieAgentServicer
from .calorie_agent_pb2 import CalorieAgentResponse


class CalorieAgentService(CalorieAgentServicer):
    def Tick(self, request, context):
        print("Tick")
        print(request)
        return CalorieAgentResponse(
            dogId=request.dogId,
        )
