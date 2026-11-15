using HealthyPaws.Services.Config;
using RabbitMQ.Client;

namespace HealthyPaws.Services.MqttPublisher;

public class MqttPublisher : IMqttPublisher, IAsyncDisposable
{
    private readonly ConnectionFactory _factory = new()
    {
        HostName = RmqConfig.Host,
        Port = RmqConfig.Port,
        UserName = RmqConfig.User,
        Password = RmqConfig.Password,
        VirtualHost = RmqConfig.VirtualHost,
        AutomaticRecoveryEnabled = true
    };
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _conn;
    private IChannel? _channel;

    public async Task Publish(string exchange, string routingKey, byte[] payload, DeliveryModes qos, CancellationToken ct = default)
    {
        var channel = await GetChannelAsync(ct);

        var props = new BasicProperties
        {
            DeliveryMode = qos
        };
        await channel.BasicPublishAsync(exchange, routingKey, mandatory: true, props, payload, ct);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _lock.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true }) return _channel;
            _conn ??= await _factory.CreateConnectionAsync(ct);
            _channel = await _conn.CreateChannelAsync(cancellationToken: ct);
            return _channel;
        }
        finally { _lock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_conn is not null) await _conn.CloseAsync();
        _lock.Dispose();
    }
}