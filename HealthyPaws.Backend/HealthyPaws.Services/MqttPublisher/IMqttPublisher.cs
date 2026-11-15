using RabbitMQ.Client;

namespace HealthyPaws.Services.MqttPublisher;

public interface IMqttPublisher
{
    public Task Publish(string exchange, string routingKey, byte[] payload, DeliveryModes qos,
        CancellationToken ct = default);
}