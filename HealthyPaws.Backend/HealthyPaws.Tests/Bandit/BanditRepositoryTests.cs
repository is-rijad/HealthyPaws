using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Services.Bandit;
using HealthyPaws.Services.FeedingService;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.Bandit;

public class BanditRepositoryTests : IDisposable
{
    static readonly DateTimeOffset Now = new(2026, 3, 10, 6, 0, 0, TimeSpan.Zero);
    static readonly DateOnly Day = new(2026, 3, 10);

    readonly TestDb _db = new();
    readonly FakeClock _clock = new(Now);

    HealthyPaws.Services.Bandit.Bandit Sut(HealthyPawsDbContext? ctx = null, IFeedingService? feeding = null) =>
        new(ctx ?? _db.Create(), feeding ?? new StubFeeding(), _clock);

    public void Dispose() => _db.Dispose();

    async Task<(Guid Id, int DogId)> Planned(int mealIndex = 0)
    {
        var dog = await _db.AddDogAsync();
        var id = Guid.NewGuid();
        Assert.True(await Sut().TryCreate(id, dog.Id, mealIndex, Day, Now.AddHours(3)));
        return (id, dog.Id);
    }

    async Task<BanditDecision> Load(Guid id) => await _db.Create().BanditDecisions.AsNoTracking().SingleAsync(x => x.Id == id);

    [Fact]
    public async Task TryCreate_inserts_planned_decision_with_timestamps()
    {
        var (id, dogId) = await Planned();
        var d = await Load(id);
        Assert.Equal(DecisionStatus.Planned, d.Status);
        Assert.Equal((dogId, 0, Day), (d.DogId, d.MealIndex, d.Day));
        Assert.Equal(Now, d.CreatedAt);
        Assert.Null(d.Arm);
    }

    [Fact]
    public async Task TryCreate_is_idempotent_per_dog_meal_and_day()
    {
        var (_, dogId) = await Planned(mealIndex: 1);
        var duplicate = await Sut().TryCreate(Guid.NewGuid(), dogId, 1, Day, Now.AddHours(3));
        Assert.False(duplicate);
        Assert.Equal(1, await _db.Create().BanditDecisions.CountAsync());
    }

    [Fact]
    public async Task TryCreate_allows_other_meal_or_other_day()
    {
        var (_, dogId) = await Planned(mealIndex: 0);
        Assert.True(await Sut().TryCreate(Guid.NewGuid(), dogId, 1, Day, Now.AddHours(8)));
        Assert.True(await Sut().TryCreate(Guid.NewGuid(), dogId, 0, Day.AddDays(1), Now.AddDays(1)));
    }

    [Fact]
    public async Task TryCreate_unknown_dog_is_not_swallowed_as_duplicate()
    {
        // FK prekršaj nije "već postoji" -> izuzetak se mora propagirati
        await Assert.ThrowsAnyAsync<Exception>(() => Sut().TryCreate(Guid.NewGuid(), 9999, 0, Day, Now));
    }

    [Fact]
    public async Task Get_returns_dto_or_null()
    {
        var (id, dogId) = await Planned();
        var dto = await Sut().Get(id);
        Assert.Equal(new DecisionDto(id, dogId, 0, Now.AddHours(3), null, DecisionStatus.Planned), dto);
        Assert.Null(await Sut().Get(Guid.NewGuid()));
    }

    [Fact]
    public async Task Transition_succeeds_only_from_expected_status()
    {
        var (id, _) = await Planned();
        Assert.False(await Sut().Transition(id, DecisionStatus.Chosen, DecisionStatus.Dispensed));
        Assert.Equal(DecisionStatus.Planned, (await Load(id)).Status);
        Assert.True(await Sut().Transition(id, DecisionStatus.Planned, DecisionStatus.Requested));
        Assert.False(await Sut().Transition(id, DecisionStatus.Planned, DecisionStatus.Requested)); // drugi put ne prolazi
    }

    [Theory]
    [InlineData(DecisionStatus.Requested)]
    [InlineData(DecisionStatus.Dispensed)]
    [InlineData(DecisionStatus.FeedbackSent)]
    [InlineData(DecisionStatus.Completed)]
    public async Task Transition_stamps_matching_timestamp(DecisionStatus to)
    {
        var (id, _) = await Planned();
        _clock.Advance(TimeSpan.FromMinutes(5));
        Assert.True(await Sut().Transition(id, DecisionStatus.Planned, to));
        var d = await Load(id);
        var stamp = to switch
        {
            DecisionStatus.Requested => d.RequestedAt, DecisionStatus.Dispensed => d.DispensedAt,
            DecisionStatus.FeedbackSent => d.FeedbackSentAt, _ => d.CompletedAt,
        };
        Assert.Equal(_clock.Now, stamp);
    }

    [Theory]
    [InlineData(DecisionStatus.Skipped)]
    [InlineData(DecisionStatus.NoData)]
    public async Task Transition_to_terminal_states_sets_status_only(DecisionStatus to)
    {
        var (id, _) = await Planned();
        Assert.True(await Sut().Transition(id, DecisionStatus.Planned, to));
        var d = await Load(id);
        Assert.Equal(to, d.Status);
        Assert.Null(d.CompletedAt);
    }

    [Fact]
    public async Task Concurrent_transitions_have_exactly_one_winner()
    {
        var (id, _) = await Planned();
        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Sut(_db.Create()).Transition(id, DecisionStatus.Planned, DecisionStatus.Requested)));
        Assert.Equal(1, results.Count(x => x));
    }

    [Fact]
    public async Task TryChoose_sets_arm_status_and_flags()
    {
        var (id, _) = await Planned();
        await Sut().Transition(id, DecisionStatus.Planned, DecisionStatus.Requested);
        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.True(await Sut().TryChoose(id, -60, [DecisionStatus.Requested]));
        var d = await Load(id);
        Assert.Equal((DecisionStatus.Chosen, -60, false, _clock.Now), (d.Status, d.Arm, d.ChosenByFallback, d.ChosenAt));
    }

    [Fact]
    public async Task TryChoose_by_fallback_is_recorded()
    {
        var (id, _) = await Planned();
        Assert.True(await Sut().TryChoose(id, 0, [DecisionStatus.Planned, DecisionStatus.Requested], byFallback: true));
        Assert.True((await Load(id)).ChosenByFallback);
    }

    [Fact]
    public async Task TryChoose_does_not_overwrite_an_already_chosen_arm()
    {
        var (id, _) = await Planned();
        Assert.True(await Sut().TryChoose(id, 60, [DecisionStatus.Planned]));
        Assert.False(await Sut().TryChoose(id, -60, [DecisionStatus.Planned, DecisionStatus.Requested], byFallback: true));
        Assert.Equal(60, (await Load(id)).Arm);
    }

    [Fact]
    public async Task TryChoose_from_disallowed_status_is_rejected()
    {
        var (id, _) = await Planned();
        Assert.False(await Sut().TryChoose(id, 0, [DecisionStatus.Requested]));
        Assert.Equal(DecisionStatus.Planned, (await Load(id)).Status);
    }

    [Fact]
    public async Task ListChosen_returns_only_chosen()
    {
        var (a, dogId) = await Planned(0);
        var b = Guid.NewGuid();
        await Sut().TryCreate(b, dogId, 1, Day, Now.AddHours(8));
        await Sut().TryChoose(a, 0, [DecisionStatus.Planned]);
        var chosen = await Sut().ListChosen();
        Assert.Equal([a], chosen.Select(x => x.Id));
    }

    [Fact]
    public async Task SaveOutcome_persists_measurements_including_null_latency()
    {
        var (id, _) = await Planned();
        await Sut().SaveOutcome(id, new OutcomeDto(120, 90, null));
        var d = await Load(id);
        Assert.Equal((120d, 90d), (d.DispensedG, d.EatenG));
        Assert.Null(d.LatencyS);
        await Sut().SaveOutcome(id, new OutcomeDto(120, 100, 42));
        Assert.Equal(42, (await Load(id)).LatencyS);
    }

    [Fact]
    public async Task MealsForDay_converts_local_baseline_to_utc_using_sarajevo_offset()
    {
        // zimi UTC+1, ljeti UTC+2
        var winter = new StubFeeding([new BanditSlot(1, 0, new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Unspecified))]);
        var summer = new StubFeeding([new BanditSlot(1, 0, new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Unspecified))]);
        var w = (await Sut(feeding: winter).MealsForDay(new DateOnly(2026, 1, 15))).Single();
        var s = (await Sut(feeding: summer).MealsForDay(new DateOnly(2026, 7, 15))).Single();
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 7, 0, 0, TimeSpan.Zero), w.BaselineUtc);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 6, 0, 0, TimeSpan.Zero), s.BaselineUtc);
        Assert.Equal((1, 0), (w.DogId, w.MealIndex));
    }

    sealed class StubFeeding(IReadOnlyList<BanditSlot>? slots = null) : IFeedingService
    {
        public Task<double> CalculateEatenToday(int dogId) => Task.FromResult(0d);
        public Task<IReadOnlyList<BanditSlot>> GetBanditSlots(DateOnly day) => Task.FromResult(slots ?? []);
        public Task<double> GetPortionGrams(int dogId, int mealIndex) => Task.FromResult(0d);
    }
}
