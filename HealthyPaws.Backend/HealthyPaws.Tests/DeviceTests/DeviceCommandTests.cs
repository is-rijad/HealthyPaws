using System.Text.Json;
using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Device;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database.Entity;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Services.Bandit;
using HealthyPaws.Services.Device;
using HealthyPaws.Services.MqttPublisher;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace HealthyPaws.Tests.DeviceTests;

public class DeviceCommandTests : IDisposable
{
    static readonly DateTimeOffset Now = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
    readonly TestDb _db = new();
    readonly FakeClock _clock = new(Now);
    readonly FakeMqtt _mqtt = new();
    readonly FakeWeights _weights = new();
    readonly FakeDevices _devices = new();
    public void Dispose() => _db.Dispose();

    DeviceCommand Sut() => new(_db.Create(), _weights, _clock, _devices, _mqtt);

    async Task<Guid> Decision(int dogId, int mealIndex = 2, DateTimeOffset? dispensedAt = null)
    {
        await using var ctx = _db.Create();
        var d = new BanditDecision
        {
            Id = Guid.NewGuid(), DogId = dogId, MealIndex = mealIndex, Day = new DateOnly(2026, 3, 10), BaselineUtc = Now,
            Status = DecisionStatus.Dispensed, CreatedAt = Now, DispensedAt = dispensedAt,
        };
        ctx.BanditDecisions.Add(d);
        await ctx.SaveChangesAsync();
        return d.Id;
    }

    [Fact]
    public async Task Dispense_publishes_to_the_devices_topic_with_decision_and_meal_index()
    {
        var dog = await _db.AddDogAsync();
        _devices.Mac = "AA:BB:CC:DD:EE:0F";
        var id = await Decision(dog.Id, mealIndex: 2);

        await Sut().Dispense(id, dog.Id);

        var message = Assert.Single(_mqtt.Published);
        Assert.Equal(("amq.topic", "dispense.aa.bb.cc.dd.ee.0f", DeliveryModes.Persistent), (message.Exchange, message.RoutingKey, message.Qos));
        var json = JsonDocument.Parse(message.Payload).RootElement;
        Assert.Equal(id, json.GetProperty("decisionId").GetGuid());
        Assert.Equal(2, json.GetProperty("mealIndex").GetInt32());
        Assert.Equal(dog.Id, _devices.LastDogId);
    }

    [Fact]
    public async Task Dispense_for_unknown_decision_fails_before_publishing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().Dispense(Guid.NewGuid(), 1));
        Assert.Empty(_mqtt.Published);
    }

    [Fact]
    public async Task Dispense_publish_failure_propagates_so_the_job_is_retried()
    {
        var dog = await _db.AddDogAsync();
        var id = await Decision(dog.Id);
        _mqtt.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().Dispense(id, dog.Id));
    }

    [Fact]
    public async Task ReadOutcome_without_dispensed_timestamp_is_an_error()
    {
        var dog = await _db.AddDogAsync();
        var id = await Decision(dog.Id, dispensedAt: null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().ReadOutcome(id));
    }

    [Fact]
    public async Task ReadOutcome_reads_scale_samples_around_the_reward_window_and_computes_outcome()
    {
        var dog = await _db.AddDogAsync();
        var at = Now.AddHours(-1);
        var id = await Decision(dog.Id, dispensedAt: at);
        var end = at + BanditOptions.RewardWindow;
        _weights.Samples =
        [
            new Sample(at.AddSeconds(30), 100), new Sample(at.AddMinutes(1), 100), new Sample(at.AddMinutes(5), 60),
            new Sample(end.AddMinutes(-1), 20), new Sample(end, 20),
        ];

        var result = await Sut().ReadOutcome(id);

        Assert.Equal(OutcomeKind.Ready, result.Kind);
        Assert.Equal(100, result.Outcome!.DispensedG);
        Assert.Equal(80, result.Outcome.EatenG);
        Assert.Equal(dog.Id, _weights.LastDogId);
        Assert.Equal(at - TimeSpan.FromMinutes(5), _weights.LastFrom);
        Assert.Equal(end + TimeSpan.FromMinutes(5), _weights.LastTo);
    }

    [Fact]
    public async Task ReadOutcome_is_not_ready_while_window_is_still_open()
    {
        var dog = await _db.AddDogAsync();
        var at = Now.AddMinutes(-10);
        var id = await Decision(dog.Id, dispensedAt: at);
        _weights.Samples = [new Sample(at.AddSeconds(30), 100), new Sample(at.AddMinutes(5), 70)];
        Assert.Equal(OutcomeKind.NotReady, (await Sut().ReadOutcome(id)).Kind);
    }

    [Fact]
    public async Task ReadOutcome_without_any_samples_is_not_dispensed_after_grace()
    {
        var dog = await _db.AddDogAsync();
        var at = Now - BanditOptions.RewardWindow - TimeSpan.FromMinutes(10);
        var id = await Decision(dog.Id, dispensedAt: at);
        Assert.Equal(OutcomeKind.NotDispensed, (await Sut().ReadOutcome(id)).Kind);
    }

    // ---------------------------------------------------------------- DeviceWeightReader

    [Fact]
    public async Task Weight_reader_returns_feeding_records_of_that_dog_inside_the_range_as_utc_samples()
    {
        var dog = await _db.AddDogAsync("A");
        var other = await _db.AddDogAsync("B");
        var plan = await AddPlan(dog.Id);
        var otherPlan = await AddPlan(other.Id);
        var from = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        await using (var ctx = _db.Create())
        {
            ctx.FeedingRecords.AddRange(
                Record(dog.Id, plan, 100, from.AddMinutes(1)),
                Record(dog.Id, plan, 60, from.AddMinutes(20)),
                Record(dog.Id, plan, 999, from.AddMinutes(-1)),   // prije
                Record(dog.Id, plan, 999, from.AddMinutes(61)),   // poslije
                Record(other.Id, otherPlan, 999, from.AddMinutes(5)));
            await ctx.SaveChangesAsync();
        }

        var samples = await new DeviceWeightReader(_db.Create()).Read(dog.Id, from, from.AddHours(1));

        Assert.Equal([100d, 60d], samples.OrderBy(s => s.Time).Select(s => s.Grams));
        Assert.All(samples, s => Assert.Equal(TimeSpan.Zero, s.Time.Offset));
    }

    [Fact]
    public async Task Weight_reader_range_is_inclusive()
    {
        var dog = await _db.AddDogAsync();
        var plan = await AddPlan(dog.Id);
        var from = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        await using (var ctx = _db.Create())
        {
            ctx.FeedingRecords.AddRange(Record(dog.Id, plan, 1, from), Record(dog.Id, plan, 2, from.AddHours(1)));
            await ctx.SaveChangesAsync();
        }
        Assert.Equal(2, (await new DeviceWeightReader(_db.Create()).Read(dog.Id, from, from.AddHours(1))).Count);
    }

    async Task<int> AddPlan(int dogId)
    {
        await using var ctx = _db.Create();
        var plan = new HealthyPaws.Database.Entity.CalorieAgent.DogPlan { DogId = dogId, DailyKcal = 1000, PlanVersion = 1 };
        ctx.DogPlans.Add(plan);
        await ctx.SaveChangesAsync();
        return plan.Id;
    }

    static FeedingRecord Record(int dogId, int planId, double grams, DateTime at) =>
        new() { DogId = dogId, PlanId = planId, EatenG = grams, OfferedG = grams, EatenAt = at };

    sealed record Published(string Exchange, string RoutingKey, byte[] Payload, DeliveryModes Qos);

    sealed class FakeMqtt : IMqttPublisher
    {
        public List<Published> Published { get; } = [];
        public bool Fail { get; set; }
        public Task Publish(string exchange, string routingKey, byte[] payload, DeliveryModes qos, CancellationToken ct = default)
        {
            if (Fail) throw new InvalidOperationException("broker nedostupan");
            Published.Add(new Published(exchange, routingKey, payload, qos));
            return Task.CompletedTask;
        }
    }

    sealed class FakeWeights : IDeviceWeightReader
    {
        public IReadOnlyList<Sample> Samples { get; set; } = [];
        public int LastDogId { get; private set; }
        public DateTimeOffset LastFrom { get; private set; }
        public DateTimeOffset LastTo { get; private set; }
        public Task<IReadOnlyList<Sample>> Read(int dogId, DateTimeOffset from, DateTimeOffset to)
        {
            (LastDogId, LastFrom, LastTo) = (dogId, from, to);
            return Task.FromResult(Samples);
        }
    }

    sealed class FakeDevices : IDeviceService
    {
        public string Mac { get; set; } = "AA:BB:CC:DD:EE:FF";
        public int LastDogId { get; private set; }
        public Task<string> GetDeviceMac(int dogId) { LastDogId = dogId; return Task.FromResult(Mac); }
        public Task<PagedResult<DeviceResponse>> GetAsync(DeviceSearchObject s) => throw new NotSupportedException();
        public Task<DeviceResponse> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<DeviceResponse> InsertAsync(DeviceInsertRequest r) => throw new NotSupportedException();
        public Task<DeviceResponse> UpdateAsync(int id, DeviceUpdateRequest r) => throw new NotSupportedException();
        public Task DeleteAsync(int id) => throw new NotSupportedException();
    }
}
