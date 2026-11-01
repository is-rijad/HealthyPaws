using EasyNetQ;

namespace HealthyPaws.Services.MqPublisher;

public class MqPublisher(IBus bus) : IMqPublisher
{
    private async Task PublishToTopicAsync<T>(string exchange, T message)
    {
        await bus.Advanced.PublishAsync(exchange, "", null, null, new Message<T>(message));
    }
}