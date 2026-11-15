namespace HealthyPaws.Services.Config;

public static class RmqConfig
{
    public static readonly string Host =  Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? throw new ArgumentException("RABBITMQ_HOST is not set!");
    public static readonly ushort Port = ushort.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? throw new ArgumentException("RABBITMQ_PORT is not set!"));
    public static readonly string User =  Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER") ?? throw new ArgumentException("RABBITMQ_DEFAULT_USER is not set!");
    public static readonly string Password =  Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS") ?? throw new ArgumentException("RABBITMQ_DEFAULT_PASS is not set!");
    public static readonly string VirtualHost =  Environment.GetEnvironmentVariable("RABBITMQ_VIRTUAL_HOST") ?? throw new ArgumentException("RABBITMQ_VIRTUAL_HOST is not set!");
}