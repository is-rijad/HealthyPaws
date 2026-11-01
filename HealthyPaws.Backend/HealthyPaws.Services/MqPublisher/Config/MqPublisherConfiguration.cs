using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyPaws.Services.MqPublisher.Config;

public static class MqPublisherConfiguration
{
    private static readonly string Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? throw new ArgumentException("RABBITMQ_HOST is not set!");
    private static readonly string Port = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? throw new ArgumentException("RABBITMQ_PORT is not set!");
    private static readonly string User = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER") ?? throw new ArgumentException("RABBITMQ_DEFAULT_USER is not set!");
    private static readonly string Pass = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS") ?? throw new ArgumentException("RABBITMQ_DEFAULT_PASS is not set!");

    public static void AddMqPublisher(this IServiceCollection services)
    {
        services.AddEasyNetQ( $"host={Host};port={Port};username={User};password={Pass}").UseSystemTextJson();
        services.AddTransient<IMqPublisher, MqPublisher>();
    }

    public static async Task BindQueues(this IServiceCollection services)
    {
        await using var scope = services.BuildServiceProvider().CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        var advancedBus = bus.Advanced;

        foreach (var binding in MqPublisherBindings.Bindings)
        {
            var exchange = await advancedBus.ExchangeDeclareAsync(binding.Key);
            var queue = await advancedBus.QueueDeclareAsync(binding.Value);

            await advancedBus.BindAsync(exchange, queue, "");
        }

    }
}