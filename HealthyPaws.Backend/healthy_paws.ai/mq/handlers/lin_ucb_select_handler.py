import os

from meal_scheduler.data.lin_ucb_config import LinUCBConfig
from meal_scheduler.processor import process_select
from mq.handlers.base_mq_handler import BaseMqHandler
from mq.publisher.mq_publisher import MqPublisher


class LinUcbSelectHandler(BaseMqHandler):
    def __init__(self):
        super().__init__(exchange_name=os.environ["RMQ_LIN_UCB_SELECT_EXCHANGE"],
                         queue_name=os.environ["RMQ_LIN_UCB_SELECT_QUEUE"],
                         routing_key=os.environ["RMQ_LIN_UCB_SELECT_ROUTING_KEY"],
                         )
        self.config = LinUCBConfig()
        self.publisher = MqPublisher()
        self.response_exchange = os.environ["RMQ_LIN_UCB_SELECTED_EXCHANGE"]
        self.response_routing_key = os.environ["RMQ_LIN_UCB_SELECTED_ROUTING_KEY"]

    async def handle_message(self, payload):
        response = process_select(payload, self.config)
        await self.publisher.publish(self.response_exchange, self.response_routing_key, response)
