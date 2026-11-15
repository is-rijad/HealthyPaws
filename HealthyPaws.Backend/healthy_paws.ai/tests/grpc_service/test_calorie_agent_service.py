from unittest.mock import MagicMock

import grpc
import pytest

from rpc.service.calorie_agent import calorie_agent_pb2 as pb
from rpc.service.calorie_agent import calorie_agent_pb2_grpc as pb_grpc
from rpc.service.calorie_agent.calorie_agent_service import CalorieAgentService
from tests.conftest import action_name, make_request


class TestServiceUnit:
    def test_tick_returns_agent_response(self):
        response = CalorieAgentService().Tick(make_request(correlation_id="c9"), MagicMock())
        assert action_name(response) == "SET_INITIAL" and response.correlation_id == "c9"

    def test_internal_error_aborts_with_status_internal(self, monkeypatch):
        import rpc.service.calorie_agent.calorie_agent_service as svc

        class Boom:
            def __init__(self, *_): ...
            def run(self, _): raise RuntimeError("kaput")

        monkeypatch.setattr(svc, "CalorieAgent", Boom)
        context = MagicMock()
        CalorieAgentService().Tick(make_request(), context)
        code, message = context.abort.call_args.args
        assert code is grpc.StatusCode.INTERNAL and "kaput" in message


@pytest.fixture
def stub():
    server = grpc.server(__import__("concurrent.futures").futures.ThreadPoolExecutor(max_workers=2))
    pb_grpc.add_CalorieAgentServicer_to_server(CalorieAgentService(), server)
    port = server.add_insecure_port("127.0.0.1:0")
    server.start()
    with grpc.insecure_channel(f"127.0.0.1:{port}") as channel:
        yield pb_grpc.CalorieAgentStub(channel)
    server.stop(0)


class TestOverRealGrpc:
    def test_roundtrip(self, stub):
        response = stub.Tick(make_request(dog_id=5))
        assert response.dog_id == 5 and action_name(response) == "SET_INITIAL"
        assert response.new_daily_kcal == pytest.approx(1271.08, abs=0.01)
        assert response.diagnostics.fields["state"].string_value == "Ideal"

    def test_invalid_input_is_a_normal_response_not_an_rpc_error(self, stub):
        response = stub.Tick(make_request(weight=-1))
        assert action_name(response) == "REQUEST_REMEASURE" and response.has_error

    def test_missing_required_message_returns_response(self, stub):
        # prazan zahtjev: validacija mora uhvatiti nevaljana polja, ne srušiti servis
        try:
            response = stub.Tick(pb.CalorieAgentRequest())
            assert response.has_error
        except grpc.RpcError as e:
            assert e.code() is grpc.StatusCode.INTERNAL
