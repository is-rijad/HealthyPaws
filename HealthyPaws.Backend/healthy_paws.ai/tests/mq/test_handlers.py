import json
from unittest.mock import AsyncMock, MagicMock

import pytest

from meal_scheduler.parsers.contract_error import ContractError
from mq.handlers import lin_ucb_feedback_handler as feedback_mod
from mq.handlers import lin_ucb_select_handler as select_mod
from mq.handlers.base_mq_handler import BaseMqHandler

ENV = {
    "RABBITMQ_HOST": "h", "RABBITMQ_PORT": "5672", "RABBITMQ_DEFAULT_USER": "u",
    "RABBITMQ_DEFAULT_PASS": "p", "RABBITMQ_VIRTUAL_HOST": "/",
    "RMQ_LIN_UCB_SELECT_EXCHANGE": "sel.ex", "RMQ_LIN_UCB_SELECT_QUEUE": "sel.q",
    "RMQ_LIN_UCB_SELECT_ROUTING_KEY": "sel.rk", "RMQ_LIN_UCB_SELECTED_EXCHANGE": "selected.ex",
    "RMQ_LIN_UCB_SELECTED_ROUTING_KEY": "selected.rk",
    "RMQ_LIN_UCB_FEEDBACK_EXCHANGE": "fb.ex", "RMQ_LIN_UCB_FEEDBACK_QUEUE": "fb.q",
    "RMQ_LIN_UCB_FEEDBACK_ROUTING_KEY": "fb.rk", "RMQ_LIN_UCB_UPDATED_EXCHANGE": "upd.ex",
    "RMQ_LIN_UCB_UPDATED_ROUTING_KEY": "upd.rk",
}


@pytest.fixture(autouse=True)
def env(monkeypatch):
    for k, v in ENV.items():
        monkeypatch.setenv(k, v)


class FakeMessage:
    """Imitira aio_pika IncomingMessage: process() kao async context manager."""

    def __init__(self, body: bytes):
        self.body = body
        self.processed_with = None
        self.rejected = False

    def process(self, requeue=True):
        self.processed_with = requeue
        message = self

        class Ctx:
            async def __aenter__(self):
                return message

            async def __aexit__(self, exc_type, exc, tb):
                message.rejected = exc_type is not None
                return False  # izuzetak se propagira

        return Ctx()


class TestBaseHandler:
    def test_dead_letter_exchange_name(self):
        h = BaseMqHandler("ex", "q", "rk")
        assert h.dl_exchange_name == "ex.dlx"

    async def test_on_message_parses_json_and_never_requeues(self):
        h = BaseMqHandler("ex", "q", "rk")
        h.handle_message = AsyncMock()
        msg = FakeMessage(b'{"a": 1}')
        await h.on_message(msg)
        h.handle_message.assert_awaited_once_with({"a": 1})
        assert msg.processed_with is False  # requeue=False -> otrovne poruke idu u DLQ
        assert msg.rejected is False

    async def test_invalid_json_propagates_so_message_is_dead_lettered(self):
        h = BaseMqHandler("ex", "q", "rk")
        msg = FakeMessage(b"not json")
        with pytest.raises(json.JSONDecodeError):
            await h.on_message(msg)
        assert msg.rejected is True

    async def test_handler_error_propagates(self):
        h = BaseMqHandler("ex", "q", "rk")
        h.handle_message = AsyncMock(side_effect=ContractError("x"))
        msg = FakeMessage(b"{}")
        with pytest.raises(ContractError):
            await h.on_message(msg)
        assert msg.rejected is True

    async def test_setup_declares_topology(self):
        h = BaseMqHandler("ex", "q", "rk")
        events, dlx, dlq, queue = MagicMock(), MagicMock(), MagicMock(), MagicMock()
        dlq.bind, queue.bind, queue.consume = AsyncMock(), AsyncMock(), AsyncMock()
        ch = MagicMock()
        ch.declare_exchange = AsyncMock(side_effect=[events, dlx])
        ch.declare_queue = AsyncMock(side_effect=[dlq, queue])

        await h.setup_handler(ch)

        ch.declare_queue.assert_any_await("q.dlq", durable=True)
        ch.declare_queue.assert_any_await("q", durable=True, arguments={"x-dead-letter-exchange": "ex.dlx"})
        dlq.bind.assert_awaited_once_with(dlx)
        queue.bind.assert_awaited_once_with(events, routing_key="rk")
        queue.consume.assert_awaited_once_with(h.on_message)


class TestSelectHandler:
    async def test_wires_env_and_publishes_result(self, monkeypatch):
        monkeypatch.setattr(select_mod, "process_select", lambda payload, cfg: b"RESULT")
        h = select_mod.LinUcbSelectHandler()
        h.publisher.publish = AsyncMock()
        assert (h.exchange_name, h.queue_name, h.routing_key) == ("sel.ex", "sel.q", "sel.rk")
        await h.handle_message(b"{}")
        h.publisher.publish.assert_awaited_once_with("selected.ex", "selected.rk", b"RESULT")

    async def test_does_not_publish_when_processing_fails(self, monkeypatch):
        def boom(*a):
            raise ContractError("x")
        monkeypatch.setattr(select_mod, "process_select", boom)
        h = select_mod.LinUcbSelectHandler()
        h.publisher.publish = AsyncMock()
        with pytest.raises(ContractError):
            await h.handle_message(b"{}")
        h.publisher.publish.assert_not_awaited()


class TestFeedbackHandler:
    async def test_wires_env_and_publishes_result(self, monkeypatch):
        monkeypatch.setattr(feedback_mod, "process_feedback", lambda payload, cfg, rcfg: b"UPDATED")
        h = feedback_mod.LinUcbFeedbackHandler()
        h.publisher.publish = AsyncMock()
        assert (h.exchange_name, h.queue_name, h.routing_key) == ("fb.ex", "fb.q", "fb.rk")
        await h.handle_message(b"{}")
        h.publisher.publish.assert_awaited_once_with("upd.ex", "upd.rk", b"UPDATED")

    async def test_missing_env_fails_fast(self, monkeypatch):
        monkeypatch.delenv("RMQ_LIN_UCB_UPDATED_EXCHANGE")
        with pytest.raises(KeyError):
            feedback_mod.LinUcbFeedbackHandler()
