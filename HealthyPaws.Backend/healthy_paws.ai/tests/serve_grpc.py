"""Pokreće CalorieAgent gRPC servis na zadanom portu (koristi ga C# E2E test: HealthyPaws.Tests)."""
import os
import sys
from concurrent import futures

# sys.path[0] je tests/ (direktorij skripte); ukloni ga da paketi iz tests/ ne zasjene produkcijske module
sys.path[:] = [p for p in sys.path if os.path.abspath(p or '.') != os.path.dirname(os.path.abspath(__file__))]

import grpc

from rpc.service.calorie_agent import calorie_agent_pb2_grpc
from rpc.service.calorie_agent.calorie_agent_service import CalorieAgentService

server = grpc.server(futures.ThreadPoolExecutor(max_workers=4))
calorie_agent_pb2_grpc.add_CalorieAgentServicer_to_server(CalorieAgentService(), server)
port = server.add_insecure_port(f"127.0.0.1:{sys.argv[1]}")
server.start()
print("READY", flush=True)
server.wait_for_termination()
