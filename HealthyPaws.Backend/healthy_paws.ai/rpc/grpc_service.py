from concurrent import futures

import grpc

from rpc.service.calorie_agent import calorie_agent_pb2_grpc
from rpc.service.calorie_agent.calorie_agent_service import CalorieAgentService


class GrpcService:
    def __init__(self):
        self.server = None

    def serve(self):
        self.server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))

        calorie_agent_pb2_grpc.add_CalorieAgentServicer_to_server(CalorieAgentService(), self.server)

        self.server.add_insecure_port("[::]:50051")
        self.server.start()

        print("Server started on port 50051")

        self.server.wait_for_termination()