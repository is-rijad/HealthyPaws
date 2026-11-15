using System.Text.Json;
using Hangfire;
using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Services.Bandit;
using HealthyPaws.Services.Bandit.Consumers;
using HealthyPaws.Services.Bandit.Data;
using HealthyPaws.Services.Bandit.Jobs;
using HealthyPaws.Tests.Infrastructure;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthyPaws.Tests.Consumers;

/// <summary>Consumeri se izvršavaju kroz stvarni MassTransit in-memory bus (test harness), s SQLite bazom i lažnim Hangfire klijentom.</summary>
public class BanditConsumerTests : IAsyncLifetime
{
    static readonly DateTimeOffset Now = new(2026, 3, 10, 6, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Baseline = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    readonly TestDb _db = new();
    readonly FakeClock _clock = new(Now);
    readonly FakeJobClient _jobs = new();
    readonly FakePublisher _pub = new();
    ServiceProvider _provider = null!;
    ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(_clock)
            .AddSingleton<IBackgroundJobClient>(_jobs)
            .AddScoped(_ => _db.Create())
            .AddScoped<IBandit>(sp => new HealthyPaws.Services.Bandit.Bandit(sp.GetRequiredService<HealthyPawsDbContext>(), new FakeFeeding(), _clock))
            .AddScoped<IBanditData>(sp => new BanditData(sp.GetRequiredService<HealthyPawsDbContext>(), _clock))
            .AddScoped(sp => new BanditJobs(sp.GetRequiredService<IBandit>(), _pub, new FakeDevice(), _jobs, _clock,
                TimeZoneInfo.FindSystemTimeZoneById(BanditOptions.TimeZone), NullLogger<BanditJobs>.Instance))
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<SelectedConsumer>();
                x.AddConsumer<FeedbackUpdatedConsumer>();
            })
            .BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
        _db.Dispose();
    }

    async Task<(Guid Id, int DogId)> Seed(DecisionStatus status, int? arm = null)
    {
        var dog = await _db.Create().Dogs.FirstOrDefaultAsync() ?? await _db.AddDogAsync();
        await using var ctx = _db.Create();
        var decision = new BanditDecision
        {
            Id = Guid.NewGuid(), DogId = dog.Id, MealIndex = 1, Day = new DateOnly(2026, 3, 10), BaselineUtc = Baseline,
            Status = status, Arm = arm, CreatedAt = Now,
        };
        ctx.BanditDecisions.Add(decision);
        await ctx.SaveChangesAsync();
        return (decision.Id, dog.Id);
    }

    async Task<BanditDecision> Load(Guid id) => await _db.Create().BanditDecisions.AsNoTracking().SingleAsync(x => x.Id == id);

    async Task WaitConsumed<T>(int count) where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (_harness.Consumed.Select<T>().Count() < count)
        {
            Assert.True(DateTime.UtcNow < deadline, $"consumer nije obradio {count} poruka");
            await Task.Delay(25);
        }
        await Task.Delay(100);   // da i zadnja poruka završi sa upisom u bazu
    }

    static JsonElement State(int version) => JsonDocument.Parse("{\"d\":7,\"version\":" + version + ",\"arms\":{}}").RootElement.Clone();

    // ------------------------------------------------------------------ SelectedConsumer

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    [InlineData(60)]
    public async Task Selected_allowed_arm_chooses_the_decision_and_schedules_dispense(int arm)
    {
        var (id, _) = await Seed(DecisionStatus.Requested);

        await _harness.Bus.Publish(new SelectResponse(id.ToString(), "1", arm, 0));
        Assert.True(await _harness.Consumed.Any<SelectResponse>());
        Assert.True(await _harness.GetConsumerHarness<SelectedConsumer>().Consumed.Any<SelectResponse>());

        var decision = await Load(id);
        Assert.Equal((DecisionStatus.Chosen, arm, false), (decision.Status, decision.Arm, decision.ChosenByFallback));
        Assert.Equal(Baseline.AddMinutes(arm), _jobs.Single(nameof(BanditJobs.Dispense)).At);
    }

    [Theory]
    [InlineData(15)]     // Python vrati arm iz drugog skupa (npr. stari config)
    [InlineData(120)]
    [InlineData(-61)]
    public async Task Selected_arm_outside_the_allowed_set_is_ignored(int arm)
    {
        var (id, _) = await Seed(DecisionStatus.Requested);
        await _harness.Bus.Publish(new SelectResponse(id.ToString(), "1", arm, 0));
        Assert.True(await _harness.Consumed.Any<SelectResponse>());

        Assert.Equal(DecisionStatus.Requested, (await Load(id)).Status);   // SelectDeadline će ga riješiti fallbackom
        Assert.Empty(_jobs.Scheduled);
    }

    [Fact]
    public async Task Selected_without_arm_is_ignored()
    {
        var (id, _) = await Seed(DecisionStatus.Requested);
        await _harness.Bus.Publish(new SelectResponse(id.ToString(), "1", null, 0));
        Assert.True(await _harness.Consumed.Any<SelectResponse>());
        Assert.Equal(DecisionStatus.Requested, (await Load(id)).Status);
    }

    [Fact]
    public async Task Late_selection_after_fallback_does_not_override_it()
    {
        var (id, _) = await Seed(DecisionStatus.Chosen, arm: 0);
        await _harness.Bus.Publish(new SelectResponse(id.ToString(), "1", 60, 0));
        Assert.True(await _harness.Consumed.Any<SelectResponse>());
        Assert.Equal(0, (await Load(id)).Arm);
        Assert.Empty(_jobs.Scheduled);
    }

    [Fact]
    public async Task Selected_message_is_idempotent_on_redelivery()
    {
        var (id, _) = await Seed(DecisionStatus.Requested);
        await _harness.Bus.Publish(new SelectResponse(id.ToString(), "1", 60, 0));
        await _harness.Bus.Publish(new SelectResponse(id.ToString(), "1", -60, 0));
        await WaitConsumed<SelectResponse>(2);

        Assert.Equal(60, (await Load(id)).Arm);                            // prvi pobjeđuje
        Assert.Single(_jobs.Scheduled, j => j.Method == nameof(BanditJobs.Dispense));
    }

    [Fact]
    public async Task Selected_with_malformed_decision_id_faults_the_consumer()
    {
        await _harness.Bus.Publish(new SelectResponse("nije-guid", "1", 0, 0));
        Assert.True(await _harness.Consumed.Any<SelectResponse>(x => x.Exception is not null));
        Assert.True(await _harness.Published.Any<Fault<SelectResponse>>());
    }

    // ------------------------------------------------------------------ FeedbackUpdatedConsumer

    [Fact]
    public async Task Feedback_applies_state_and_completes_the_decision()
    {
        var (id, dogId) = await Seed(DecisionStatus.FeedbackSent, arm: 60);

        await _harness.Bus.Publish(new FeedbackResponse(id.ToString(), dogId.ToString(), 0.8, State(1)));
        await WaitConsumed<FeedbackResponse>(1);
        Assert.All(_harness.Consumed.Select<FeedbackResponse>(), c => Assert.Null(c.Exception));

        var decision = await Load(id);
        Assert.Equal((DecisionStatus.Completed, 0.8), (decision.Status, decision.Reward));
        var (version, json) = await _provider.CreateScope().ServiceProvider.GetRequiredService<IBanditData>().GetState(dogId);
        Assert.Equal(1, version);
        Assert.Equal(1, JsonDocument.Parse(json!).RootElement.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Feedback_chains_onto_existing_state_version()
    {
        var (id, dogId) = await Seed(DecisionStatus.FeedbackSent, arm: 0);
        await new BanditData(_db.Create(), _clock).TryApplyState(dogId, 0, "{\"version\":1}");

        await _harness.Bus.Publish(new FeedbackResponse(id.ToString(), dogId.ToString(), 0.5, State(2)));
        Assert.True(await _harness.Consumed.Any<FeedbackResponse>());

        Assert.Equal(2, (await new BanditData(_db.Create(), _clock).GetState(dogId)).Version);
        Assert.Equal(DecisionStatus.Completed, (await Load(id)).Status);
    }

    [Fact]
    public async Task Feedback_with_version_conflict_requeues_the_outcome_collection()
    {
        // Python je izračunao novo stanje na verziji 1, ali je u međuvremenu drugi obrok već pomjerio bazu na 2
        var (id, dogId) = await Seed(DecisionStatus.FeedbackSent, arm: 0);
        var data = new BanditData(_db.Create(), _clock);
        await data.TryApplyState(dogId, 0, "{\"version\":1}");
        await new BanditData(_db.Create(), _clock).TryApplyState(dogId, 1, "{\"version\":2}");

        await _harness.Bus.Publish(new FeedbackResponse(id.ToString(), dogId.ToString(), 0.5, State(2)));
        Assert.True(await _harness.Consumed.Any<FeedbackResponse>());

        var decision = await Load(id);
        Assert.Equal(DecisionStatus.Dispensed, decision.Status);           // feedback se računa ponovo nad novim stanjem
        Assert.Null(decision.Reward);
        Assert.Equal(2, (await new BanditData(_db.Create(), _clock).GetState(dogId)).Version);   // stanje nije pokvareno
        Assert.Single(_jobs.Scheduled, j => j.Method == nameof(BanditJobs.CollectOutcome));
    }

    [Fact]
    public async Task Duplicate_feedback_delivery_changes_nothing()
    {
        var (id, dogId) = await Seed(DecisionStatus.FeedbackSent, arm: 0);
        var message = new FeedbackResponse(id.ToString(), dogId.ToString(), 0.7, State(1));
        await _harness.Bus.Publish(message);
        await _harness.Bus.Publish(message);
        await WaitConsumed<FeedbackResponse>(2);

        Assert.Equal(1, (await new BanditData(_db.Create(), _clock).GetState(dogId)).Version);
        Assert.Equal((DecisionStatus.Completed, 0.7), ((await Load(id)).Status, (await Load(id)).Reward));
        Assert.Empty(_jobs.Scheduled);                                      // nema ponovnog skupljanja za završenu odluku
    }

    [Fact]
    public async Task Feedback_for_decision_not_awaiting_feedback_is_dropped_without_touching_state()
    {
        var (id, dogId) = await Seed(DecisionStatus.Dispensed, arm: 0);
        await _harness.Bus.Publish(new FeedbackResponse(id.ToString(), dogId.ToString(), 0.7, State(1)));
        Assert.True(await _harness.Consumed.Any<FeedbackResponse>());
        Assert.Equal(DecisionStatus.Dispensed, (await Load(id)).Status);
        Assert.Equal(0, (await new BanditData(_db.Create(), _clock).GetState(dogId)).Version);
    }

    [Fact]
    public async Task Feedback_without_state_version_faults()
    {
        var (id, dogId) = await Seed(DecisionStatus.FeedbackSent, arm: 0);
        var noVersion = JsonDocument.Parse("""{"d":7,"arms":{}}""").RootElement.Clone();
        await _harness.Bus.Publish(new FeedbackResponse(id.ToString(), dogId.ToString(), 0.7, noVersion));
        Assert.True(await _harness.Consumed.Any<FeedbackResponse>(x => x.Exception is not null));
        Assert.Equal(DecisionStatus.FeedbackSent, (await Load(id)).Status);
    }
}
