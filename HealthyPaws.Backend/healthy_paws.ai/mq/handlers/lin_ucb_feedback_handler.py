import os

from meal_scheduler.data.lin_ucb_config import LinUCBConfig
from meal_scheduler.data.reward_config import RewardConfig
from meal_scheduler.processor import process_feedback
from mq.handlers.base_mq_handler import BaseMqHandler
from mq.publisher.mq_publisher import MqPublisher


class LinUcbFeedbackHandler(BaseMqHandler):
    def __init__(self):
        super().__init__(exchange_name=os.environ["RMQ_LIN_UCB_FEEDBACK_EXCHANGE"],
                         queue_name=os.environ["RMQ_LIN_UCB_FEEDBACK_QUEUE"],
                         routing_key=os.environ["RMQ_LIN_UCB_FEEDBACK_ROUTING_KEY"],
                         )
        self.config = LinUCBConfig()
        self.reward_config = RewardConfig()
        self.publisher = MqPublisher()
        self.response_exchange = os.environ["RMQ_LIN_UCB_UPDATED_EXCHANGE"]
        self.response_routing_key = os.environ["RMQ_LIN_UCB_UPDATED_ROUTING_KEY"]

    async def handle_message(self, payload):
        response = process_feedback(payload, self.config, self.reward_config)
        await self.publisher.publish(self.response_exchange, self.response_routing_key, response)
