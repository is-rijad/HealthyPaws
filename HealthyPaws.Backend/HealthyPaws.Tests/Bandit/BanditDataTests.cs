using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Database.Entity;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.Bandit.Data;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.Bandit;

public class BanditDataTests : IDisposable
{
    // 10.03.2026 09:00 UTC = 10:00 u Sarajevu (CET)
    static readonly DateTimeOffset Baseline = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);
    static readonly DateOnly Day = new(2026, 3, 10);

    readonly TestDb _db = new();
    readonly FakeClock _clock = new(new DateTimeOffset(2026, 3, 10, 5, 0, 0, TimeSpan.Zero));

    BanditData Sut() => new(_db.Create(), _clock);
    public void Dispose() => _db.Dispose();

    async Task<Guid> AddDecision(int dogId, int meal, DateTimeOffset baseline, Action<BanditDecision>? configure = null)
    {
        await using var ctx = _db.Create();
        var d = new BanditDecision
        {
            Id = Guid.NewGuid(), DogId = dogId, MealIndex = meal, Day = DateOnly.FromDateTime(baseline.UtcDateTime),
            BaselineUtc = baseline, Status = DecisionStatus.Planned, CreatedAt = _clock.Now,
        };
        configure?.Invoke(d);
        ctx.BanditDecisions.Add(d);
        await ctx.SaveChangesAsync();
        return d.Id;
    }

    // ---------------- BuildContext ----------------

    [Fact]
    public async Task BuildContext_for_a_new_dog_has_no_history()
    {
        var dog = await _db.AddDogAsync();
        var id = await AddDecision(dog.Id, 2, Baseline);
        var (dogId, meal, ctx) = await Sut().BuildContext(id);
        Assert.Equal((dog.Id, 2), (dogId, meal));
        Assert.Null(ctx.HoursSinceLastMeal);
        Assert.Null(ctx.AvgEatenFraction);
        Assert.Null(ctx.TreatBudgetShare);   // nema plana
        Assert.Equal(10.0, ctx.BaselineHour); // lokalno vrijeme, ne UTC
        Assert.Equal(2, ctx.MealIndex);
    }

    [Fact]
    public async Task BuildContext_hours_since_last_dispense()
    {
        var dog = await _db.AddDogAsync();
        await AddDecision(dog.Id, 0, Baseline.AddHours(-5), d => { d.DispensedAt = Baseline.AddHours(-5); d.DispensedG = 100; });
        await AddDecision(dog.Id, 1, Baseline.AddHours(-3), d => { d.DispensedAt = Baseline.AddHours(-2.5); d.DispensedG = 100; });
        var id = await AddDecision(dog.Id, 2, Baseline);
        var (_, _, ctx) = await Sut().BuildContext(id);
        Assert.Equal(2.5, ctx.HoursSinceLastMeal);
    }

    [Fact]
    public async Task BuildContext_ignores_meals_with_zero_dispensed_and_future_decisions()
    {
        var dog = await _db.AddDogAsync();
        await AddDecision(dog.Id, 0, Baseline.AddHours(-3), d => { d.DispensedAt = Baseline.AddHours(-3); d.DispensedG = 0; });
        await AddDecision(dog.Id, 1, Baseline.AddHours(5), d => { d.DispensedAt = Baseline.AddHours(5); d.DispensedG = 100; });
        var id = await AddDecision(dog.Id, 2, Baseline);
        var (_, _, ctx) = await Sut().BuildContext(id);
        Assert.Null(ctx.HoursSinceLastMeal);
    }

    [Fact]
    public async Task BuildContext_average_eaten_fraction_uses_last_five_and_clamps()
    {
        var dog = await _db.AddDogAsync();
        // najstariji (ne ulazi): 0 %, zatim pet novijih: 100 %, 50 %, 150 % (clamp 100 %), 50 %, 100 %
        await AddDecision(dog.Id, 0, Baseline.AddHours(-30), d => { d.DispensedG = 100; d.EatenG = 0; });
        var eaten = new[] { 100.0, 50.0, 150.0, 50.0, 100.0 };
        for (var i = 0; i < eaten.Length; i++)
        {
            var e = eaten[i];
            await AddDecision(dog.Id, i + 1, Baseline.AddHours(-25 + i * 4), d => { d.DispensedG = 100; d.EatenG = e; });
        }
        var id = await AddDecision(dog.Id, 3, Baseline);
        var (_, _, ctx) = await Sut().BuildContext(id);
        Assert.Equal((1.0 + 0.5 + 1.0 + 0.5 + 1.0) / 5, ctx.AvgEatenFraction!.Value, 9);
    }

    [Fact]
    public async Task BuildContext_average_is_per_dog()
    {
        var dog = await _db.AddDogAsync("A");
        var other = await _db.AddDogAsync("B");
        await AddDecision(other.Id, 0, Baseline.AddHours(-3), d => { d.DispensedG = 100; d.EatenG = 0; });
        var id = await AddDecision(dog.Id, 1, Baseline);
        var (_, _, ctx) = await Sut().BuildContext(id);
        Assert.Null(ctx.AvgEatenFraction);
    }

    [Fact]
    public async Task BuildContext_treat_share_is_last_24h_treat_calories_over_daily_plan_clamped()
    {
        var dog = await _db.AddDogAsync();
        await using (var ctx = _db.Create())
        {
            ctx.DogPlans.Add(new DogPlan { DogId = dog.Id, DailyKcal = 1000, DailyGrams = 300, PlanVersion = 1 });
            var at = Baseline.UtcDateTime;
            ctx.TreatLogs.AddRange(
                new TreatLog { DogId = dog.Id, TreatName = "a", Quantity = 1, Calories = 60, GivenAt = at.AddHours(-2) },
                new TreatLog { DogId = dog.Id, TreatName = "b", Quantity = 1, Calories = 40, GivenAt = at.AddHours(-23) },
                new TreatLog { DogId = dog.Id, TreatName = "old", Quantity = 1, Calories = 500, GivenAt = at.AddHours(-25) },
                new TreatLog { DogId = dog.Id, TreatName = "future", Quantity = 1, Calories = 500, GivenAt = at.AddMinutes(1) });
            await ctx.SaveChangesAsync();
        }
        var id = await AddDecision(dog.Id, 0, Baseline);
        var (_, _, built) = await Sut().BuildContext(id);
        Assert.Equal(0.1, built.TreatBudgetShare!.Value, 9);
    }

    [Fact]
    public async Task BuildContext_treat_share_is_capped_at_one_and_zero_without_treats()
    {
        var dog = await _db.AddDogAsync();
        await using (var ctx = _db.Create())
        {
            ctx.DogPlans.Add(new DogPlan { DogId = dog.Id, DailyKcal = 100, PlanVersion = 1 });
            ctx.TreatLogs.Add(new TreatLog { DogId = dog.Id, TreatName = "x", Quantity = 1, Calories = 900, GivenAt = Baseline.UtcDateTime.AddHours(-1) });
            await ctx.SaveChangesAsync();
        }
        var withTreats = await Sut().BuildContext(await AddDecision(dog.Id, 0, Baseline));
        Assert.Equal(1.0, withTreats.Context.TreatBudgetShare);

        var dog2 = await _db.AddDogAsync("B");
        await using (var ctx = _db.Create()) { ctx.DogPlans.Add(new DogPlan { DogId = dog2.Id, DailyKcal = 100, PlanVersion = 1 }); await ctx.SaveChangesAsync(); }
        var none = await Sut().BuildContext(await AddDecision(dog2.Id, 0, Baseline));
        Assert.Equal(0.0, none.Context.TreatBudgetShare);
    }

    [Fact]
    public async Task BuildContext_baseline_hour_follows_dst()
    {
        var dog = await _db.AddDogAsync();
        var summer = await AddDecision(dog.Id, 0, new DateTimeOffset(2026, 7, 10, 9, 30, 0, TimeSpan.Zero)); // 11:30 CEST
        var (_, _, ctx) = await Sut().BuildContext(summer);
        Assert.Equal(11.5, ctx.BaselineHour);
    }

    // ---------------- context cache ----------------

    [Fact]
    public async Task GetContext_builds_once_then_serves_the_saved_snapshot()
    {
        var dog = await _db.AddDogAsync();
        var id = await AddDecision(dog.Id, 0, Baseline);
        var first = await Sut().GetContext(id);
        Assert.Null(first.AvgEatenFraction);

        // novi podaci poslije snapshota ne smiju promijeniti kontekst (isti ulaz za select i feedback)
        await AddDecision(dog.Id, 3, Baseline.AddHours(-2), d => { d.DispensedG = 100; d.EatenG = 100; d.DispensedAt = Baseline.AddHours(-2); });
        var second = await Sut().GetContext(id);
        Assert.Equal(first, second);
        Assert.NotNull((await _db.Create().BanditDecisions.SingleAsync(x => x.Id == id)).ContextSavedAt);
    }

    [Fact]
    public async Task SaveContext_roundtrip()
    {
        var dog = await _db.AddDogAsync();
        var id = await AddDecision(dog.Id, 0, Baseline);
        var saved = new BanditContext(3.5, 0.8, 0.05, 12.25, 0);
        await Sut().SaveContext(id, saved);
        Assert.Equal(saved, await Sut().GetContext(id));
    }

    // ---------------- state (optimistic concurrency) ----------------

    [Fact]
    public async Task GetState_for_unknown_dog_is_version_zero_without_json()
    {
        var dog = await _db.AddDogAsync();
        Assert.Equal((0, (string?)null), await Sut().GetState(dog.Id));
    }

    [Fact]
    public async Task First_state_is_created_with_version_one()
    {
        var dog = await _db.AddDogAsync();
        Assert.True(await Sut().TryApplyState(dog.Id, 0, "{\"v\":1}"));
        Assert.Equal((1, "{\"v\":1}"), await Sut().GetState(dog.Id));
    }

    [Fact]
    public async Task State_update_requires_matching_version_and_increments_it()
    {
        var dog = await _db.AddDogAsync();
        await Sut().TryApplyState(dog.Id, 0, "{\"v\":1}");
        Assert.False(await Sut().TryApplyState(dog.Id, 5, "{\"v\":\"stale\"}"));
        Assert.True(await Sut().TryApplyState(dog.Id, 1, "{\"v\":2}"));
        Assert.Equal((2, "{\"v\":2}"), await Sut().GetState(dog.Id));
    }

    [Fact]
    public async Task Creating_state_twice_with_version_zero_second_loses()
    {
        var dog = await _db.AddDogAsync();
        Assert.True(await Sut().TryApplyState(dog.Id, 0, "{\"a\":1}"));
        Assert.False(await Sut().TryApplyState(dog.Id, 0, "{\"b\":2}"));
        Assert.Equal("{\"a\":1}", (await Sut().GetState(dog.Id)).Json);
    }

    [Fact]
    public async Task Concurrent_state_writers_exactly_one_wins()
    {
        var dog = await _db.AddDogAsync();
        await Sut().TryApplyState(dog.Id, 0, "{}");
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Sut().TryApplyState(dog.Id, 1, $"{{\"w\":{i}}}")));
        Assert.Equal(1, results.Count(x => x));
        Assert.Equal(2, (await Sut().GetState(dog.Id)).Version);
    }

    // ---------------- feedback ----------------

    [Fact]
    public async Task Feedback_updates_state_and_completes_decision_atomically()
    {
        var dog = await _db.AddDogAsync();
        var id = await AddDecision(dog.Id, 0, Baseline, d => d.Status = DecisionStatus.FeedbackSent);
        Assert.True(await Sut().TryApplyFeedback(id, dog.Id, 0, "{\"s\":1}", 0.75));

        var decision = await _db.Create().BanditDecisions.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal((DecisionStatus.Completed, 0.75, _clock.Now), (decision.Status, decision.Reward, decision.CompletedAt));
        Assert.Equal(1, (await Sut().GetState(dog.Id)).Version);
    }

    [Fact]
    public async Task Feedback_with_stale_version_changes_nothing()
    {
        var dog = await _db.AddDogAsync();
        await Sut().TryApplyState(dog.Id, 0, "{\"s\":1}");
        var id = await AddDecision(dog.Id, 0, Baseline, d => d.Status = DecisionStatus.FeedbackSent);

        Assert.False(await Sut().TryApplyFeedback(id, dog.Id, 0, "{\"s\":\"stale\"}", 0.5));

        Assert.Equal((1, "{\"s\":1}"), await Sut().GetState(dog.Id));
        var d = await _db.Create().BanditDecisions.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(DecisionStatus.FeedbackSent, d.Status);
        Assert.Null(d.Reward);
    }

    [Fact]
    public async Task Feedback_for_decision_not_in_feedback_sent_rolls_back_state_change()
    {
        var dog = await _db.AddDogAsync();
        var id = await AddDecision(dog.Id, 0, Baseline, d => d.Status = DecisionStatus.Dispensed);

        Assert.False(await Sut().TryApplyFeedback(id, dog.Id, 0, "{\"s\":1}", 0.5));

        Assert.Equal((0, (string?)null), await Sut().GetState(dog.Id));  // transakcija je poništena
    }

    [Fact]
    public async Task Feedback_applied_twice_is_idempotent()
    {
        var dog = await _db.AddDogAsync();
        var id = await AddDecision(dog.Id, 0, Baseline, d => d.Status = DecisionStatus.FeedbackSent);
        Assert.True(await Sut().TryApplyFeedback(id, dog.Id, 0, "{\"s\":1}", 0.5));
        Assert.False(await Sut().TryApplyFeedback(id, dog.Id, 1, "{\"s\":2}", 0.9)); // isti dolazni event, ali odluka je već Completed
        Assert.Equal(1, (await Sut().GetState(dog.Id)).Version);
    }

    // ---------------- ostalo ----------------

    [Fact]
    public async Task GetDecisionArm_returns_dog_and_arm_or_fails_when_no_arm()
    {
        var dog = await _db.AddDogAsync();
        var chosen = await AddDecision(dog.Id, 0, Baseline, d => d.Arm = -60);
        var none = await AddDecision(dog.Id, 1, Baseline.AddHours(4));
        Assert.Equal((dog.Id, -60), await Sut().GetDecisionArm(chosen));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().GetDecisionArm(none));
        Assert.Equal(dog.Id, await Sut().GetDogId(none));
    }
}
