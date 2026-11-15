import asyncio, signal
import os

import grpc, aio_pika

from mq.handlers.lin_ucb_feedback_handler import LinUcbFeedbackHandler
from mq.handlers.lin_ucb_select_handler import LinUcbSelectHandler
from rpc.service.calorie_agent import calorie_agent_pb2_grpc
from rpc.service.calorie_agent.calorie_agent_service import CalorieAgentService

async def run_grpc(stop: asyncio.Event):
    server = grpc.aio.server()

    calorie_agent_pb2_grpc.add_CalorieAgentServicer_to_server(CalorieAgentService(), server)
    server.add_insecure_port(f"[::]:{os.environ['RPC_LISTEN_PORT']}")

    await server.start()
    print(f"GRPC server is listening on port {os.environ['RPC_LISTEN_PORT']}")
    await stop.wait()
    await server.stop(grace=5)

async def run_consumer(stop: asyncio.Event):
    conn = await aio_pika.connect_robust(host=os.environ["RABBITMQ_HOST"],
                                         port=int(os.environ["RABBITMQ_PORT"]),
                                         login=os.environ["RABBITMQ_DEFAULT_USER"],
                                         password=os.environ["RABBITMQ_DEFAULT_PASS"],
                                         virtual_host=os.environ["RABBITMQ_VIRTUAL_HOST"],
    )
    async with conn:
        ch = await conn.channel()
        await ch.set_qos(prefetch_count=10)

        await LinUcbSelectHandler().setup_handler(ch)
        await LinUcbFeedbackHandler().setup_handler(ch)

        print(f"Start consuming {os.environ['RABBITMQ_HOST']}")
        await stop.wait()

async def main():
    stop = asyncio.Event()
    loop = asyncio.get_running_loop()
    for s in (signal.SIGINT, signal.SIGTERM):
        loop.add_signal_handler(s, stop.set)
    async with asyncio.TaskGroup() as tg:
        tg.create_task(run_grpc(stop))
        tg.create_task(run_consumer(stop))

asyncio.run(main())