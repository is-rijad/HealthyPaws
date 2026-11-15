import json

from aio_pika.abc import AbstractIncomingMessage, AbstractChannel, ExchangeType

class BaseMqHandler:
    def __init__(self, exchange_name, queue_name, routing_key):
        self.exchange_name = exchange_name
        self.queue_name = queue_name
        self.routing_key = routing_key

        self.dl_exchange_name = f'{exchange_name}.dlx'

    async def setup_handler(self, ch: AbstractChannel) -> None:
        events = await ch.declare_exchange(self.exchange_name, ExchangeType.TOPIC, durable=True)
        dlx = await ch.declare_exchange(self.dl_exchange_name, ExchangeType.FANOUT, durable=True)

        dlq = await ch.declare_queue(f"{self.queue_name}.dlq", durable=True)
        await dlq.bind(dlx)

        q = await ch.declare_queue(
            self.queue_name,
            durable=True,
            arguments={"x-dead-letter-exchange": self.dl_exchange_name},
        )
        await q.bind(events, routing_key=self.routing_key)
        await q.consume(self.on_message)

    async def on_message(self, msg: AbstractIncomingMessage) -> None:
        async with msg.process(requeue=False):
            payload = json.loads(msg.body)
            await self.handle_message(payload)

    async def handle_message(self, payload):
        pass