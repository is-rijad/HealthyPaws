using Hangfire;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.Bandit;
using HealthyPaws.Services.Bandit.Consumers;
using HealthyPaws.Services.Bandit.Data;
using HealthyPaws.Services.Bandit.Jobs;
using HealthyPaws.Services.Bandit.Publisher;
using HealthyPaws.Services.Breed;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.Device;
using HealthyPaws.Services.Dog;
using HealthyPaws.Services.DogWeight;
using HealthyPaws.Services.FeedingService;
using HealthyPaws.Services.MqttPublisher;
using HealthyPaws.Services.Notifications;
using HealthyPaws.Services.Treat;
using HealthyPaws.Services.TreatLog;
using HealthyPaws.Services.User;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyPaws.Services.Config;

internal class BanditJobsRegistrar(IRecurringJobManager recurringJobs, IBackgroundJobClient jobs, TimeZoneInfo timeZone) : Microsoft.Extensions.Hosting.IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobs.AddOrUpdate<BanditJobs>("bandit-plan-today", j => j.PlanToday(), "5 0 * * *",
            new RecurringJobOptions { TimeZone = timeZone });
        recurringJobs.AddOrUpdate<BanditJobs>("bandit-reconcile", j => j.Reconcile(), "*/5 * * * *",
            new RecurringJobOptions { TimeZone = timeZone });
        jobs.Enqueue<BanditJobs>(j => j.PlanToday());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class ServicesConfiguration
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddScoped<AuthorizationService>();
        services.AddScoped<TokenManager>();

        services.AddSingleton<RpcClient.RpcClient>();
        services.AddSingleton<IMqttPublisher, MqttPublisher.MqttPublisher>();

        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IDogService, DogService>();
        services.AddScoped<IBreedService, BreedService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IDogWeightService, DogWeightService>();
        services.AddScoped<ITreatService, TreatService>();
        services.AddScoped<ITreatLogService, TreatLogService>();
        services.AddScoped<IFeedingService, FeedingService.FeedingService>();

        services.AddScoped<ICalorieAgentService, CalorieAgentService>();

        services.AddScoped<INotificationService, NotificationService>();

        services.AddHangfire();

        services.AddScoped<IBandit, Bandit.Bandit>();
        services.AddScoped<IBanditData, BanditData>();
        services.AddScoped<IDeviceCommand, DeviceCommand>();
        services.AddScoped<IBanditPublisher, BanditPublisher>();

        services.AddScoped<IDeviceWeightReader, DeviceWeightReader>();
    }

    private static void AddHangfire(this IServiceCollection services)
    {
        services.AddHangfire(c =>
            c.UseSqlServerStorage(Environment.GetEnvironmentVariable("ConnectionStrings__Default")));
        services.AddHangfireServer();
        services.AddScoped<BanditJobs>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(BanditOptions.TimeZone));
        services.AddHostedService<BanditJobsRegistrar>();
        
        
        services.AddMassTransit(x =>
        {
            x.AddConsumer<SelectedConsumer>();
            x.AddConsumer<FeedbackUpdatedConsumer>();
            x.UsingRabbitMq((ctx, rmq) =>
            {
                rmq.Host(RmqConfig.Host, RmqConfig.Port, RmqConfig.VirtualHost, c =>
                {
                    c.Username(RmqConfig.User);
                    c.Password(RmqConfig.Password);
                });
                rmq.ReceiveEndpoint(Environment.GetEnvironmentVariable("RMQ_LIN_UCB_SELECTED_QUEUE") ?? throw new ArgumentException("RMQ_LIN_UCB_SELECTED_QUEUE is not set!"), e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.UseRawJsonDeserializer(RawSerializerOptions.AddTransportHeaders);
                    e.ConfigureConsumer<SelectedConsumer>(ctx);
                });
                rmq.ReceiveEndpoint(Environment.GetEnvironmentVariable("RMQ_LIN_UCB_UPDATED_QUEUE") ?? throw new ArgumentException("RMQ_LIN_UCB_UPDATED_QUEUE is not set!"), e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.UseRawJsonDeserializer(RawSerializerOptions.AddTransportHeaders);
                    e.ConfigureConsumer<FeedbackUpdatedConsumer>(ctx);
                });
            });
        });
    }
}