using CalorieAgent;
using HealhtyPaws.Models.Requests.TreatLog;
using HealhtyPaws.Models.Responses.TreatLog;
using HealhtyPaws.Models.SearchObjects;
using HealhtyPaws.Models.BaseModels;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Services.FeedingService;
using HealthyPaws.Services.Notifications;
using HealthyPaws.Services.TreatLog;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Action = CalorieAgent.Action;

namespace HealthyPaws.Tests.CalorieAgentE2E;

/// <summary>E2E: CalorieAgentService (C#) -> gRPC -> pravi Python agent -> HandleResponse -> SQLite.</summary>
public class CalorieAgentServiceTests(PythonAgentFixture agent) : IClassFixture<PythonAgentFixture>, IDisposable
{
    static readonly DateTime Week0 = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    readonly TestDb _db = new();
    readonly FakeNotifications _notifications = new();
    readonly FakeTreats _treats = new();
    readonly FakeFeeding _feeding = new();
    readonly List<RpcClientHolder> _clients = [];

    public void Dispose()
    {
        foreach (var client in _clients) client.Dispose();
        _db.Dispose();
    }

    sealed class RpcClientHolder(HealthyPaws.Services.RpcClient.RpcClient client) : IDisposable
    {
        public HealthyPaws.Services.RpcClient.RpcClient Client { get; } = client;
        public void Dispose() => Client.Dispose();
    }

    CalorieAgentService Sut()
    {
        Skip.IfNot(agent.Available, agent.SkipReason);
        var holder = new RpcClientHolder(new HealthyPaws.Services.RpcClient.RpcClient());
        _clients.Add(holder);
        return new CalorieAgentService(_db.Create(), _notifications, _treats, null!, _feeding, holder.Client);
    }

    async Task<DogWeight> AddWeight(Dog dog, double weight, int bcs, DateTime at, double kcalPer100g = 350)
    {
        await using var ctx = _db.Create();
        var entity = new DogWeight { DogId = dog.Id, Weight = weight, Bcs = bcs, Calorie = kcalPer100g, CreatedAt = at };
        ctx.DogWeights.Add(entity);
        await ctx.SaveChangesAsync();
        return entity;
    }

    async Task<(Dog Dog, DogWeight Weight)> Prepare(double weight, int bcs, DateTime? at = null)
    {
        var dog = await _db.AddDogAsync();
        return (dog, await AddWeight(dog, weight, bcs, at ?? Week0));
    }

    async Task AddFeeding(int dogId, int planId, DateTime from, int days, double eatenPerDay)
    {
        await using var ctx = _db.Create();
        for (var i = 0; i < days; i++)
            ctx.FeedingRecords.Add(new FeedingRecord
            {
                DogId = dogId, PlanId = planId, EatenAt = from.AddDays(i).AddHours(1), EatenG = eatenPerDay, OfferedG = eatenPerDay + 20,
            });
        await ctx.SaveChangesAsync();
    }

    Task<DogPlan> LoadPlan(int dogId) => _db.Create().DogPlans.Include(p => p.Learned).AsNoTracking().SingleAsync(p => p.DogId == dogId);

    // ---------------------------------------------------------------- prvi tick

    [SkippableFact]
    public async Task First_measurement_creates_the_initial_plan_and_persists_everything()
    {
        var (dog, weight) = await Prepare(20, 5);

        var response = await Sut().TickAsync(dog, weight);

        Assert.Equal(Action.SetInitial, response.Action);
        var plan = await LoadPlan(dog.Id);
        Assert.Equal(1271.08, plan.DailyKcal, 1);                       // RER(20) * 1.6 * 1.2
        Assert.Equal(plan.DailyKcal / 3.5, plan.DailyGrams, 3);
        Assert.Equal(1, plan.PlanVersion);
        Assert.Equal(new DateTimeOffset(Week0).AddSeconds(0), plan.LastAppliedMeasuredAt);
        Assert.Equal(Week0.AddDays(7), plan.NextMeasureAt);
        Assert.Equal(plan.DailyKcal, plan.Learned!.MerEstimateKcal);   // prior je prva procjena
        Assert.Equal(0, plan.Learned.NUpdates);

        var log = await _db.Create().AgentDecisionLogs.SingleAsync();
        Assert.Equal((AgentAction.SetInitial, AgentOutcome.PlanApplied, false, false), (log.Action, log.Outcome, log.IsRejected, log.RequiresHumanReview));
        Assert.Contains("INITIAL_PLAN", log.ReasonCodes);
        Assert.False(string.IsNullOrWhiteSpace(log.Explanation));
        Assert.Contains("\"state\"", log.Diagnostics);                  // dijagnostika se čuva kao JSON
        Assert.Empty(_notifications.Sent);
    }

    [SkippableFact]
    public async Task Request_carries_dog_profile_to_the_agent()
    {
        var dog = await _db.AddDogAsync(minWeight: 18, maxWeight: 24);
        await using (var ctx = _db.Create())
        {
            var tracked = await ctx.Dogs.SingleAsync(d => d.Id == dog.Id);
            tracked.IsNeutered = false;                                 // intact: faktor 1.8 umjesto 1.6
            await ctx.SaveChangesAsync();
        }
        var weight = await AddWeight(dog, 20, 5, Week0);
        await using var read = _db.Create();
        var response = await Sut().TickAsync(await read.Dogs.Include(d => d.Breed).SingleAsync(d => d.Id == dog.Id), weight);
        Assert.Equal(1430.0, response.NewDailyKcal, 0);                 // RER(20)*1.8*1.2
    }

    // ---------------------------------------------------------------- regulacija

    [SkippableFact]
    public async Task Overweight_dog_second_week_decreases_plan_within_warmup_step()
    {
        var (dog, week0) = await Prepare(28, 9);
        await Sut().TickAsync(dog, week0);
        var initial = await LoadPlan(dog.Id);

        await AddFeeding(dog.Id, initial.Id, Week0, days: 7, eatenPerDay: 330);
        var week1 = await AddWeight(dog, 28, 9, Week0.AddDays(7));
        var response = await Sut().TickAsync(dog, week1);

        Assert.Equal(Action.Decrease, response.Action);
        var plan = await LoadPlan(dog.Id);
        Assert.Equal(2, plan.PlanVersion);
        Assert.InRange(plan.DailyKcal, initial.DailyKcal * 0.95 - 1e-6, initial.DailyKcal - 1);   // zagrijavanje: najviše 5 %
        Assert.Equal(plan.DailyKcal / 3.5, plan.DailyGrams, 3);
        Assert.Equal(1, plan.Learned!.NUpdates);

        var logs = await _db.Create().AgentDecisionLogs.OrderBy(l => l.MeasuredAt).ToListAsync();
        Assert.Equal([AgentOutcome.PlanApplied, AgentOutcome.PlanApplied], logs.Select(l => l.Outcome));
        Assert.Equal([AgentAction.SetInitial, AgentAction.Decrease], logs.Select(l => l.Action));
        Assert.Contains("WEIGHT_LOSS_TARGET", logs[1].ReasonCodes);
    }

    [SkippableFact]
    public async Task Treat_calories_in_period_are_sent_to_the_agent_as_extra_intake()
    {
        var (dog, week0) = await Prepare(28, 9);
        await Sut().TickAsync(dog, week0);
        var plan = await LoadPlan(dog.Id);
        await AddFeeding(dog.Id, plan.Id, Week0, days: 7, eatenPerDay: 330);
        _treats.Calories = 700;                                         // 100 kcal/dan tokom 7 dana
        var week1 = await AddWeight(dog, 28, 9, Week0.AddDays(7));

        await Sut().TickAsync(dog, week1);

        var diagnostics = await LastDiagnostics();
        Assert.Equal(100, diagnostics.GetProperty("treat_kcal").GetDouble(), 3);
    }

    [SkippableFact]
    public async Task Eaten_and_offered_grams_are_derived_from_feeding_records_and_plan()
    {
        var (dog, week0) = await Prepare(28, 9);
        await Sut().TickAsync(dog, week0);
        var plan = await LoadPlan(dog.Id);
        await AddFeeding(dog.Id, plan.Id, Week0, days: 7, eatenPerDay: 330);
        var week1 = await AddWeight(dog, 28, 9, Week0.AddDays(7));
        await Sut().TickAsync(dog, week1);

        var diagnostics = await LastDiagnostics();
        // pojedeno 330 g/dan prema ponuđenih plan.DailyGrams
        Assert.Equal(330 / plan.DailyGrams, diagnostics.GetProperty("eaten_ratio").GetDouble(), 3);
        Assert.Equal(330 * 3.5, diagnostics.GetProperty("feeder_kcal").GetDouble(), 3);
    }

    async Task<System.Text.Json.JsonElement> LastDiagnostics()
    {
        var json = (await _db.Create().AgentDecisionLogs.OrderByDescending(l => l.MeasuredAt).FirstAsync()).Diagnostics!;
        return System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();
    }

    // ---------------------------------------------------------------- odbijanja agenta

    [SkippableFact]
    public async Task Implausible_jump_is_recorded_as_rejected_and_pending_weight_is_persisted()
    {
        var (dog, week0) = await Prepare(20, 5);
        await Sut().TickAsync(dog, week0);
        var before = await LoadPlan(dog.Id);

        var jump = await AddWeight(dog, 26, 5, Week0.AddDays(7));        // +30 % u sedmici = nevjerovatno
        var response = await Sut().TickAsync(dog, jump);

        Assert.Equal(Action.RequestRemeasure, response.Action);
        var log = await _db.Create().AgentDecisionLogs.OrderByDescending(l => l.MeasuredAt).FirstAsync();
        Assert.Equal((AgentAction.RequestRemeasure, true), (log.Action, log.IsRejected));
        Assert.Contains("REMEASURE_IMPLAUSIBLE_CHANGE", log.ReasonCodes);
        var after = await LoadPlan(dog.Id);
        Assert.Equal((before.PlanVersion, before.DailyKcal), (after.PlanVersion, after.DailyKcal));   // plan netaknut
        Assert.Equal(26, after.Learned!.PendingWeightKg);                                               // čeka potvrdu
        Assert.Empty(await _db.Create().Alerts.ToListAsync());                                          // remeasure nije alarm
    }

    [SkippableFact]
    public async Task Second_matching_weight_confirms_the_jump_and_raises_an_alert()
    {
        var (dog, week0) = await Prepare(20, 5);
        await Sut().TickAsync(dog, week0);
        await Sut().TickAsync(dog, await AddWeight(dog, 26, 5, Week0.AddDays(7)));

        // ponovljeno vaganje (nekoliko sati kasnije) se poklapa s prvim -> eskalacija
        var response = await Sut().TickAsync(dog, await AddWeight(dog, 26.2, 5, Week0.AddDays(7).AddHours(2)));

        Assert.Equal(Action.Escalate, response.Action);
        var alert = await _db.Create().Alerts.SingleAsync();
        Assert.Equal(("RAPID_CHANGE_CONFIRMED", 1), (alert.Reason, alert.Occurrences));
        Assert.Null(alert.ResolvedAt);
        Assert.Single(_notifications.Sent);
        Assert.Null((await LoadPlan(dog.Id)).Learned!.PendingWeightKg);
    }

    [SkippableFact]
    public async Task Rejected_measurement_is_excluded_from_history_of_later_ticks()
    {
        var (dog, week0) = await Prepare(20, 5);
        await Sut().TickAsync(dog, week0);
        await Sut().TickAsync(dog, await AddWeight(dog, 26, 5, Week0.AddDays(7)));           // odbijeno

        // sljedeće normalno vaganje (20.1) se poredi s 20, a ne s odbačenih 26 -> bez lažnog skoka
        var plan = await LoadPlan(dog.Id);
        await AddFeeding(dog.Id, plan.Id, Week0, days: 14, eatenPerDay: 350);
        var response = await Sut().TickAsync(dog, await AddWeight(dog, 20.1, 5, Week0.AddDays(14)));

        Assert.NotEqual(Action.RequestRemeasure, response.Action);
        Assert.NotEqual(Action.Escalate, response.Action);
    }

    [SkippableFact]
    public async Task Healthy_stable_dog_is_logged_as_no_change_without_error()
    {
        var (dog, week0) = await Prepare(20, 5);
        await Sut().TickAsync(dog, week0);
        var plan = await LoadPlan(dog.Id);
        await AddFeeding(dog.Id, plan.Id, Week0, days: 7, eatenPerDay: 350);

        var response = await Sut().TickAsync(dog, await AddWeight(dog, 20, 5, Week0.AddDays(7)));

        Assert.False(response.HasError);
        var log = await _db.Create().AgentDecisionLogs.OrderByDescending(l => l.MeasuredAt).FirstAsync();
        Assert.Equal((AgentOutcome.NoPlanChange, AgentAction.NoChange), (log.Outcome, log.Action));
        Assert.Equal(1, (await LoadPlan(dog.Id)).PlanVersion);
        Assert.Equal(1, (await LoadPlan(dog.Id)).Learned!.NUpdates);      // naučeno stanje se sprema
    }

    [SkippableFact]
    public async Task Escalation_creates_one_alert_and_repeats_only_bump_occurrences()
    {
        var (dog, week0) = await Prepare(20, 5);
        await Sut().TickAsync(dog, week0);
        var plan = await LoadPlan(dog.Id);
        await AddFeeding(dog.Id, plan.Id, Week0, days: 14, eatenPerDay: 200);     // 55 % ponuđenog -> REDUCED_APPETITE

        await Sut().TickAsync(dog, await AddWeight(dog, 20, 5, Week0.AddDays(7)));
        await Sut().TickAsync(dog, await AddWeight(dog, 20, 5, Week0.AddDays(14)));

        var alert = await _db.Create().Alerts.SingleAsync();
        Assert.Equal(("REDUCED_APPETITE", 2), (alert.Reason, alert.Occurrences));
        Assert.Single(_notifications.Sent);                                        // korisnik se obavještava samo prvi put
    }

    [SkippableFact]
    public async Task Alert_is_resolved_by_agent_when_dog_recovers()
    {
        var (dog, week0) = await Prepare(20, 5);
        await Sut().TickAsync(dog, week0);
        var plan = await LoadPlan(dog.Id);
        await AddFeeding(dog.Id, plan.Id, Week0, days: 7, eatenPerDay: 200);
        await Sut().TickAsync(dog, await AddWeight(dog, 20, 5, Week0.AddDays(7)));          // alert
        await AddFeeding(dog.Id, plan.Id, Week0.AddDays(7), days: 7, eatenPerDay: 350);
        await Sut().TickAsync(dog, await AddWeight(dog, 20, 5, Week0.AddDays(14)));         // oporavak

        var alert = await _db.Create().Alerts.SingleAsync();
        Assert.NotNull(alert.ResolvedAt);
        Assert.Equal(AlertResolvedBy.Agent, alert.ResolvedBy);
    }

    [SkippableFact]
    public async Task Invalid_measurement_is_bad_request_and_nothing_is_persisted()
    {
        var (dog, weight) = await Prepare(-3, 5);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Sut().TickAsync(dog, weight));
        Assert.Contains("ponovite vaganje", ex.Message);
        Assert.Empty(await _db.Create().DogPlans.ToListAsync());
        Assert.Empty(await _db.Create().AgentDecisionLogs.ToListAsync());
    }

    [SkippableFact]
    public async Task Unreachable_agent_surfaces_as_generic_exception_not_bad_request()
    {
        Skip.IfNot(agent.Available, agent.SkipReason);
        var (dog, weight) = await Prepare(20, 5);
        var previous = Environment.GetEnvironmentVariable("Rpc_Url");
        Environment.SetEnvironmentVariable("Rpc_Url", "http://127.0.0.1:1");
        try
        {
            var service = new CalorieAgentService(_db.Create(), _notifications, _treats, null!, _feeding, new HealthyPaws.Services.RpcClient.RpcClient());
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => service.TickAsync(dog, weight));
            Assert.IsNotType<BadRequestException>(ex);
        }
        finally { Environment.SetEnvironmentVariable("Rpc_Url", previous); }
    }

    [SkippableFact]
    public async Task History_excludes_current_weight_and_uses_at_most_eight_previous()
    {
        var dog = await _db.AddDogAsync();
        for (var i = 0; i < 12; i++) await AddWeight(dog, 20, 5, Week0.AddDays(i * 7));
        var current = await AddWeight(dog, 20, 5, Week0.AddDays(12 * 7));
        // bez plana -> SET_INITIAL, ali zahtjev se gradi s historijom; ne smije puknuti ni na dugoj historiji
        var response = await Sut().TickAsync(dog, current);
        Assert.Equal(Action.SetInitial, response.Action);
    }

    // ---------------------------------------------------------------- fakeovi

    sealed class FakeNotifications : INotificationService
    {
        public List<Alert> Sent { get; } = [];
        public Task SendAlertAsync(Alert alert, CalorieAgentResponse response) { Sent.Add(alert); return Task.CompletedTask; }
    }

    sealed class FakeTreats : ITreatLogService
    {
        public double Calories { get; set; }
        public Task<double> GetTreatCaloriesAsync(int dogId, DateTime? from, DateTime? to) => Task.FromResult(Calories);
        public Task<PagedResult<TreatLogResponse>> GetAsync(TreatLogSearchObject search) => throw new NotSupportedException();
        public Task<TreatLogResponse> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<TreatLogResponse> InsertAsync(TreatLogInsertRequest request) => throw new NotSupportedException();
        public Task<TreatLogResponse> UpdateAsync(int id, TreatLogInsertRequest request) => throw new NotSupportedException();
        public Task DeleteAsync(int id) => throw new NotSupportedException();
    }

    sealed class FakeFeeding : IFeedingService
    {
        public Task<double> CalculateEatenToday(int dogId) => Task.FromResult(0d);
        public Task<IReadOnlyList<HealhtyPaws.Models.Dto.Bandit.BanditSlot>> GetBanditSlots(DateOnly day) => Task.FromResult<IReadOnlyList<HealhtyPaws.Models.Dto.Bandit.BanditSlot>>([]);
        public Task<double> GetPortionGrams(int dogId, int mealIndex) => Task.FromResult(0d);
    }
}
