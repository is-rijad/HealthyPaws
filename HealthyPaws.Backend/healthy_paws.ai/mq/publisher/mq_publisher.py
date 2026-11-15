import asyncio
import os

import aio_pika


class MqPublisher:
    def __init__(self):
        self.host=os.environ["RABBITMQ_HOST"]
        self.port=int(os.environ["RABBITMQ_PORT"])
        self.login=os.environ["RABBITMQ_DEFAULT_USER"]
        self.password=os.environ["RABBITMQ_DEFAULT_PASS"]
        self.virtual_host=os.environ["RABBITMQ_VIRTUAL_HOST"]
        self.loop = asyncio.get_event_loop()

    async def publish(self, exchange_name:str, routing_key:str, body:bytes):
        connection = await aio_pika.connect_robust(
            username=self.login,
            password=self.password,
            host=self.host,
            port=self.port,
            virtual_host=self.virtual_host,
            loop=self.loop
        )

        async with connection:
            channel = await connection.channel()
            exchange = await channel.get_exchange(exchange_name)
            await exchange.publish(
                aio_pika.Message(
                    body=body
                ),
                routing_key=routing_key
            )
