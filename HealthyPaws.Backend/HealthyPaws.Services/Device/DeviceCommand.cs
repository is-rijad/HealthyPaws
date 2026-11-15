using System.Text.Json;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Database;
using HealthyPaws.Services.Bandit;
using HealthyPaws.Services.Bandit.OutcomeCalculator;
using HealthyPaws.Services.MqttPublisher;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace HealthyPaws.Services.Device;

public sealed class DeviceCommand(HealthyPawsDbContext dbContext, IDeviceWeightReader weights, TimeProvider clock,
    IDeviceService devices, IMqttPublisher mqtt) : IDeviceCommand
{
    static readonly TimeSpan RewardWindow = BanditOptions.RewardWindow; 
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
 
    public async Task Dispense(Guid decisionId, int dogId)
    {
        var mealIndex = await dbContext.BanditDecisions.AsNoTracking().Where(x => x.Id == decisionId)
            .Select(x => x.MealIndex).SingleAsync();
 
        var deviceId = (await devices.GetDeviceMac(dogId)).Replace(":", ".").ToLower();
 
        var payload = JsonSerializer.SerializeToUtf8Bytes(new { decisionId, mealIndex }, Json);
        await mqtt.Publish("amq.topic", $"dispense.{deviceId}", payload, qos: DeliveryModes.Persistent);   // topic uskladi s onim što ESP32 sluša
    }

    
    public Task<OutcomeResult> ReadOutcome(Guid decisionId) => Load(decisionId);

    async Task<OutcomeResult> Load(Guid decisionId)
    {
        var decisions = await dbContext.BanditDecisions.AsNoTracking().Where(x => x.Id == decisionId)
            .Select(x => new { x.DogId, x.DispensedAt }).SingleAsync();
        var at = decisions.DispensedAt ?? throw new InvalidOperationException($"Odluka {decisionId} nema DispensedAt.");

        var samples = await weights.Read(decisions.DogId, at - TimeSpan.FromMinutes(5), at + RewardWindow + TimeSpan.FromMinutes(5));
        return OutcomeCalculator.Compute(samples, at, RewardWindow, clock.GetUtcNow());
    }
}