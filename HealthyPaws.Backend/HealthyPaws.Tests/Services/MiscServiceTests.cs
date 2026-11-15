using CalorieAgent;
using Google.Protobuf.WellKnownTypes;
using HealthyPaws.Database.Entity;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.FeedingService;
using HealthyPaws.Services.Mapster;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.Services;

public class LearnedStateProtoExtensionTests
{
    [Fact]
    public void Maps_all_fields_including_optional_ones()
    {
        var proto = new CalorieAgent.LearnedState
        {
            MerEstimateKcal = 1234.5, VarianceKcal2 = 400, NUpdates = 4, ConsecutiveBadWeeks = 2, WeeksAtBound = 1,
            PendingWeightKg = 21.3, LastUpdateAt = Timestamp.FromDateTimeOffset(new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero)),
        };
        var entity = proto.ToEntity();
        Assert.Equal((1234.5, 400d, 4, 2, 1, 21.3), (entity.MerEstimateKcal, entity.VarianceKcal2, entity.NUpdates,
            entity.ConsecutiveBadWeeks, entity.WeeksAtBound, entity.PendingWeightKg));
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero), entity.LastUpdateAt);
    }

    [Fact]
    public void Unset_optionals_become_null_instead_of_zero()
    {
        var entity = new CalorieAgent.LearnedState { NUpdates = 1 }.ToEntity();
        Assert.Null(entity.MerEstimateKcal);
        Assert.Null(entity.VarianceKcal2);
        Assert.Null(entity.PendingWeightKg);
        Assert.Null(entity.LastUpdateAt);
    }

    [Fact]
    public void CopyFrom_clears_previously_set_values_when_agent_no_longer_reports_them()
    {
        // npr. pending_weight_kg se briše nakon potvrde druge težine
        var entity = new HealthyPaws.Database.Entity.CalorieAgent.LearnedState { PendingWeightKg = 26, MerEstimateKcal = 1000 };
        entity.CopyFrom(new CalorieAgent.LearnedState { MerEstimateKcal = 1100, VarianceKcal2 = 50 });
        Assert.Null(entity.PendingWeightKg);
        Assert.Equal(1100, entity.MerEstimateKcal);
    }
}

public class FeedingServiceTests : IDisposable
{
    readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    FeedingService Sut() => new(_db.Create());

    async Task<int> AddPlan(int dogId, double kcal)
    {
        await using var ctx = _db.Create();
        var plan = new DogPlan { DogId = dogId, DailyKcal = kcal, DailyGrams = kcal / 3.5, PlanVersion = 1 };
        ctx.DogPlans.Add(plan);
        await ctx.SaveChangesAsync();
        return plan.Id;
    }

    async Task AddMeal(int dogId, int index, int hour, int minute = 0)
    {
        await using var ctx = _db.Create();
        ctx.DogMeals.Add(new DogMeal { DogId = dogId, MealIndex = (short)index, Time = new TimeOnly(hour, minute) });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Bandit_slots_are_meals_of_dogs_with_a_plan_shifted_by_baseline_offset()
    {
        var withPlan = await _db.AddDogAsync("A");
        var withoutPlan = await _db.AddDogAsync("B");
        await AddPlan(withPlan.Id, 1000);
        await AddMeal(withPlan.Id, 0, 8);
        await AddMeal(withPlan.Id, 1, 18, 30);
        await AddMeal(withoutPlan.Id, 0, 9);

        var slots = (await Sut().GetBanditSlots(new DateOnly(2026, 3, 10))).OrderBy(s => s.MealIndex).ToList();

        Assert.Equal(2, slots.Count);
        Assert.All(slots, s => Assert.Equal(withPlan.Id, s.DogId));
        Assert.Equal(new DateTime(2026, 3, 10, 9, 0, 0), slots[0].LocalBaseline);   // 08:00 + 60 min
        Assert.Equal(new DateTime(2026, 3, 10, 19, 30, 0), slots[1].LocalBaseline);
        Assert.Equal(DateTimeKind.Unspecified, slots[0].LocalBaseline.Kind);
    }

    [Fact]
    public async Task Late_meal_baseline_rolls_over_to_next_day()
    {
        var dog = await _db.AddDogAsync();
        await AddPlan(dog.Id, 1000);
        await AddMeal(dog.Id, 0, 23, 30);
        var slot = Assert.Single(await Sut().GetBanditSlots(new DateOnly(2026, 3, 10)));
        Assert.Equal(new DateTime(2026, 3, 11, 0, 30, 0), slot.LocalBaseline);   // Bandit.MealsForDay/PlanToday to moraju znati
    }

    [Fact]
    public async Task Portion_is_daily_kcal_split_across_meals()
    {
        var dog = await _db.AddDogAsync();
        await AddPlan(dog.Id, 1200);
        await AddMeal(dog.Id, 0, 8);
        await AddMeal(dog.Id, 1, 14);
        await AddMeal(dog.Id, 2, 20);
        Assert.Equal(400, await Sut().GetPortionGrams(dog.Id, 0));
    }

    [Fact]
    public async Task Portion_without_meals_is_a_clear_error_not_infinity()
    {
        var dog = await _db.AddDogAsync();
        await AddPlan(dog.Id, 1200);
        await Assert.ThrowsAsync<HealthyPaws.Services.Exceptions.BadRequestException>(() => Sut().GetPortionGrams(dog.Id, 0));
    }

    [Fact]
    public async Task Portion_without_plan_is_not_found()
    {
        var dog = await _db.AddDogAsync();
        await Assert.ThrowsAsync<HealthyPaws.Services.Exceptions.NotFoundException>(() => Sut().GetPortionGrams(dog.Id, 0));
    }

    [Fact]
    public async Task Eaten_today_sums_only_todays_records_for_that_dog_and_rounds()
    {
        var dog = await _db.AddDogAsync("A");
        var other = await _db.AddDogAsync("B");
        var planId = await AddPlan(dog.Id, 1000);
        var otherPlan = await AddPlan(other.Id, 1000);
        var today = DateTime.UtcNow.Date;
        await using (var ctx = _db.Create())
        {
            ctx.FeedingRecords.AddRange(
                Record(dog.Id, planId, 100.04, today.AddHours(1)),
                Record(dog.Id, planId, 50.03, today.AddHours(2)),
                Record(dog.Id, planId, 999, today.AddDays(-1).AddHours(23)),
                Record(other.Id, otherPlan, 999, today.AddHours(1)));
            await ctx.SaveChangesAsync();
        }
        Assert.Equal(150.1, await Sut().CalculateEatenToday(dog.Id));
    }

    static FeedingRecord Record(int dogId, int planId, double eaten, DateTime at) =>
        new() { DogId = dogId, PlanId = planId, EatenG = eaten, OfferedG = eaten, EatenAt = at };
}
