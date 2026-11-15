using HealhtyPaws.Models.Requests.Dog;
using HealhtyPaws.Models.Requests.TreatLog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database.Entity;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.Dog;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Services.FeedingService;
using HealthyPaws.Services.TreatLog;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.Services;

public class TreatLogServiceTests : IDisposable
{
    readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    TreatLogService Sut(int? userId)
    {
        var ctx = _db.Create();
        return new TreatLogService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId));
    }

    async Task<(Dog Dog, User Owner, Treat Treat)> Setup(double kcalPerPiece = 25)
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        await using var ctx = _db.Create();
        var treat = new Treat { UserId = owner.Id, Name = "Kost", CaloriesPerPiece = kcalPerPiece };
        ctx.Treats.Add(treat);
        await ctx.SaveChangesAsync();
        return (dog, owner, treat);
    }

    [Fact]
    public async Task Insert_calculates_calories_from_quantity_and_snapshots_treat_name()
    {
        var (dog, owner, treat) = await Setup(kcalPerPiece: 12.345);
        var saved = await Sut(owner.Id).InsertAsync(new TreatLogInsertRequest { DogId = dog.Id, TreatId = treat.Id, Quantity = 3 });
        Assert.Equal(37.04, saved.Calories, 2);   // zaokruženo na 2 decimale
        Assert.Equal("Kost", saved.TreatName);
        var row = await _db.Create().TreatLogs.SingleAsync();
        Assert.Equal(treat.Id, row.TreatId);
    }

    [Fact]
    public async Task Calories_stay_correct_after_treat_is_later_renamed_or_changed()
    {
        var (dog, owner, treat) = await Setup(10);
        await Sut(owner.Id).InsertAsync(new TreatLogInsertRequest { DogId = dog.Id, TreatId = treat.Id, Quantity = 2 });
        await using (var ctx = _db.Create())
        {
            var t = await ctx.Treats.SingleAsync();
            t.CaloriesPerPiece = 1000; t.Name = "Drugo";
            await ctx.SaveChangesAsync();
        }
        var log = await _db.Create().TreatLogs.SingleAsync();
        Assert.Equal((20d, "Kost"), (log.Calories, log.TreatName));
    }

    [Fact]
    public async Task Insert_for_foreign_dog_is_not_found()
    {
        var (dog, _, _) = await Setup();
        var (_, stranger, strangerTreat) = await Setup();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut(stranger.Id).InsertAsync(new TreatLogInsertRequest { DogId = dog.Id, TreatId = strangerTreat.Id, Quantity = 1 }));
    }

    [Fact]
    public async Task Insert_with_someone_elses_treat_is_not_found()
    {
        var (dog, owner, _) = await Setup();
        var (_, _, foreignTreat) = await Setup();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut(owner.Id).InsertAsync(new TreatLogInsertRequest { DogId = dog.Id, TreatId = foreignTreat.Id, Quantity = 1 }));
    }

    [Fact]
    public async Task Insert_in_future_is_rejected_but_five_minute_tolerance_is_ok()
    {
        var (dog, owner, treat) = await Setup();
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Sut(owner.Id).InsertAsync(new TreatLogInsertRequest { DogId = dog.Id, TreatId = treat.Id, Quantity = 1, GivenAt = DateTime.UtcNow.AddMinutes(30) }));
        await Sut(owner.Id).InsertAsync(new TreatLogInsertRequest { DogId = dog.Id, TreatId = treat.Id, Quantity = 1, GivenAt = DateTime.UtcNow.AddMinutes(3) });
    }

    [Fact]
    public async Task Treat_calories_window_is_half_open_and_per_dog_and_rounded()
    {
        var (dog, owner, _) = await Setup();
        var other = await _db.AddDogAsync("Drugi");
        var from = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        await using (var ctx = _db.Create())
        {
            ctx.TreatLogs.AddRange(
                Log(dog.Id, 10.04, from),                        // početak je uključen
                Log(dog.Id, 20.06, from.AddHours(23)),
                Log(dog.Id, 999, from.AddDays(1)),               // kraj je isključen
                Log(dog.Id, 999, from.AddSeconds(-1)),
                Log(other.Id, 999, from.AddHours(1)));
            await ctx.SaveChangesAsync();
        }
        Assert.Equal(30.1, await Sut(owner.Id).GetTreatCaloriesAsync(dog.Id, from, null));
        Assert.Equal(10.0, await Sut(owner.Id).GetTreatCaloriesAsync(dog.Id, from, from.AddHours(1)));
        Assert.Equal(0, await Sut(owner.Id).GetTreatCaloriesAsync(dog.Id, from.AddYears(1), null));
    }

    [Fact]
    public async Task Get_returns_only_owners_logs_newest_first_with_filters()
    {
        var (dog, owner, _) = await Setup();
        var (otherDog, _, _) = await Setup();
        var now = DateTime.UtcNow;
        await using (var ctx = _db.Create())
        {
            ctx.TreatLogs.AddRange(Log(dog.Id, 1, now.AddDays(-3)), Log(dog.Id, 2, now.AddDays(-1)), Log(otherDog.Id, 3, now.AddDays(-1)));
            await ctx.SaveChangesAsync();
        }
        var all = await Sut(owner.Id).GetAsync(new TreatLogSearchObject());
        Assert.Equal([2d, 1d], all.Items.Select(l => l.Calories));
        var recent = await Sut(owner.Id).GetAsync(new TreatLogSearchObject { From = now.AddDays(-2) });
        Assert.Equal([2d], recent.Items.Select(l => l.Calories));
    }

    [Fact]
    public async Task Delete_requires_ownership()
    {
        var (dog, owner, treat) = await Setup();
        var saved = await Sut(owner.Id).InsertAsync(new TreatLogInsertRequest { DogId = dog.Id, TreatId = treat.Id, Quantity = 1 });
        var (_, stranger, _) = await Setup();
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).DeleteAsync(saved.Id));
        await Sut(owner.Id).DeleteAsync(saved.Id);
        Assert.Empty(await _db.Create().TreatLogs.ToListAsync());
    }

    static TreatLog Log(int dogId, double kcal, DateTime at) =>
        new() { DogId = dogId, TreatName = "t", Quantity = 1, Calories = kcal, GivenAt = at };
}

public class DogServiceTests : IDisposable
{
    readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    DogService Sut(int? userId)
    {
        var ctx = _db.Create();
        return new DogService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId), new StubCalorie());
    }

    [Fact]
    public async Task List_contains_only_dogs_reachable_through_the_users_devices()
    {
        var mine = await _db.AddDogAsync("Moj");
        var theirs = await _db.AddDogAsync("Tuđi");
        var owner = await _db.AddOwnerAsync(mine);
        await _db.AddOwnerAsync(theirs, "other@test.ba");

        var result = await Sut(owner.Id).GetAsync(new DogSearchObject());
        Assert.Equal(["Moj"], result.Items.Select(d => d.Name));
    }

    [Fact]
    public async Task Name_filter_matches_substring()
    {
        // Case-insensitivnost dolazi iz collation-a kolone Name (Croatian_CI_AS) na SQL Serveru; SQLite je ne emulira.
        var rex = await _db.AddDogAsync("Rex");
        var owner = await _db.AddOwnerAsync(rex);
        var bela = await _db.AddDogAsync("Bela");
        await using (var ctx = _db.Create())
        {
            ctx.Devices.Add(new Device { DogId = bela.Id, MacAddress = "11:22:33:44:55:77" });
            await ctx.SaveChangesAsync();
            ctx.UserDevices.Add(new UserDevice { UserId = owner.Id, DeviceId = (await ctx.Devices.SingleAsync(d => d.DogId == bela.Id)).Id });
            await ctx.SaveChangesAsync();
        }
        Assert.Equal(["Rex"], (await Sut(owner.Id).GetAsync(new DogSearchObject { Contains = "ex" })).Items.Select(d => d.Name));
        Assert.Equal(["Bela"], (await Sut(owner.Id).GetAsync(new DogSearchObject { Contains = "el" })).Items.Select(d => d.Name));
    }

    [Fact]
    public async Task Newest_dog_comes_first()
    {
        var a = await _db.AddDogAsync("Rex");
        var owner = await _db.AddOwnerAsync(a);
        var b = await _db.AddDogAsync("Reksi");
        await using (var ctx = _db.Create())
        {
            ctx.Devices.Add(new Device { DogId = b.Id, MacAddress = "11:22:33:44:55:66" });
            await ctx.SaveChangesAsync();
            ctx.UserDevices.Add(new UserDevice { UserId = owner.Id, DeviceId = (await ctx.Devices.SingleAsync(d => d.DogId == b.Id)).Id });
            await ctx.SaveChangesAsync();
        }
        var all = await Sut(owner.Id).GetAsync(new DogSearchObject());
        Assert.Equal(["Reksi", "Rex"], all.Items.Select(d => d.Name));
    }

    [Fact]
    public async Task Update_foreign_dog_is_not_found_and_changes_nothing()
    {
        var dog = await _db.AddDogAsync("Tuđi");
        await _db.AddOwnerAsync(dog);
        var stranger = await _db.AddOwnerAsync(await _db.AddDogAsync("Moj"), "x@test.ba");
        var request = new DogInsertRequest("Preimenovan", dog.BreedId, DateTime.UtcNow.AddYears(-2), 0, 1.2f, true);
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).UpdateAsync(dog.Id, request));
        Assert.Equal("Tuđi", (await _db.Create().Dogs.SingleAsync(d => d.Id == dog.Id)).Name);
    }

    [Fact]
    public async Task Update_owned_dog_changes_fields_and_returns_breed()
    {
        var dog = await _db.AddDogAsync("Rex");
        var owner = await _db.AddOwnerAsync(dog);
        var request = new DogInsertRequest("Maks", dog.BreedId, DateTime.UtcNow.AddYears(-5), 1, 1.5f, false);
        var updated = await Sut(owner.Id).UpdateAsync(dog.Id, request);
        Assert.Equal("Maks", updated.Name);
        Assert.NotNull(updated.Breed);
        var row = await _db.Create().Dogs.SingleAsync(d => d.Id == dog.Id);
        Assert.Equal((1.5f, false), (row.ActivityStatus, row.IsNeutered));
    }

    [Theory]
    [InlineData(100, "Puppy")]
    [InlineData(365, "Puppy")]
    [InlineData(366, "Adult")]
    [InlineData(2555, "Adult")]
    [InlineData(2556, "Senior")]
    public void Life_stage_boundaries(int ageDays, string expected)
    {
        var dog = new Dog { DateOfBirth = DateTime.Now.Date.AddDays(-ageDays) };
        Assert.Equal(expected, dog.LifeStage);
    }

    sealed class StubCalorie : ICalorieAgentService
    {
        public Task<CalorieAgent.CalorieAgentResponse> TickAsync(Dog dog, DogWeight weight) => throw new NotSupportedException();
        public Task<HealhtyPaws.Models.Responses.Calorie.CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to) =>
            Task.FromResult(new HealhtyPaws.Models.Responses.Calorie.CalorieBudgetResponse(dogId, 1000, 50, 400));
    }
}

public class CalorieBudgetTests : IDisposable
{
    readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    CalorieAgentService Sut(int userId, double treatKcal = 0, double eatenToday = 0)
    {
        var ctx = _db.Create();
        return new CalorieAgentService(ctx, new Notifications(), new Treats(treatKcal), TestServices.Auth(ctx, userId), new Feeding(eatenToday), null!);
    }

    [Fact]
    public async Task Budget_is_plan_minus_treats_and_eaten_food()
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        await AddPlan(dog.Id, 1234.56);

        var budget = await Sut(owner.Id, treatKcal: 120.5, eatenToday: 300.25).GetCalorieBudgetAsync(dog.Id, null, null);

        Assert.Equal(1234.6, budget.DailyBudget);          // zaokruženo na 1 decimalu
        Assert.Equal((120.5, 300.25), (budget.TreatCalories, budget.FoodCalories));
        Assert.Equal(Math.Round(1234.6 - 120.5 - 300.25, 1), budget.Remaining);
    }

    [Fact]
    public async Task Budget_can_go_negative_when_dog_overeats()
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        await AddPlan(dog.Id, 500);
        Assert.True((await Sut(owner.Id, 300, 400).GetCalorieBudgetAsync(dog.Id, null, null)).Remaining < 0);
    }

    [Fact]
    public async Task Budget_for_foreign_dog_is_not_found()
    {
        var dog = await _db.AddDogAsync();
        await _db.AddOwnerAsync(dog);
        await AddPlan(dog.Id, 500);
        var stranger = await _db.AddOwnerAsync(await _db.AddDogAsync("Moj"), "s@test.ba");
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).GetCalorieBudgetAsync(dog.Id, null, null));
    }

    [Fact]
    public async Task Budget_without_plan_is_not_found_not_a_server_error()
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(owner.Id).GetCalorieBudgetAsync(dog.Id, null, null));
    }

    async Task AddPlan(int dogId, double kcal)
    {
        await using var ctx = _db.Create();
        ctx.DogPlans.Add(new DogPlan { DogId = dogId, DailyKcal = kcal, DailyGrams = kcal / 3.5, PlanVersion = 1 });
        await ctx.SaveChangesAsync();
    }

    sealed class Notifications : HealthyPaws.Services.Notifications.INotificationService
    {
        public Task SendAlertAsync(Alert alert, CalorieAgent.CalorieAgentResponse response) => Task.CompletedTask;
    }

    sealed class Treats(double kcal) : ITreatLogService
    {
        public Task<double> GetTreatCaloriesAsync(int dogId, DateTime? from, DateTime? to) => Task.FromResult(kcal);
        public Task<HealhtyPaws.Models.BaseModels.PagedResult<HealhtyPaws.Models.Responses.TreatLog.TreatLogResponse>> GetAsync(TreatLogSearchObject s) => throw new NotSupportedException();
        public Task<HealhtyPaws.Models.Responses.TreatLog.TreatLogResponse> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<HealhtyPaws.Models.Responses.TreatLog.TreatLogResponse> InsertAsync(TreatLogInsertRequest r) => throw new NotSupportedException();
        public Task<HealhtyPaws.Models.Responses.TreatLog.TreatLogResponse> UpdateAsync(int id, TreatLogInsertRequest r) => throw new NotSupportedException();
        public Task DeleteAsync(int id) => throw new NotSupportedException();
    }

    sealed class Feeding(double eaten) : IFeedingService
    {
        public Task<double> CalculateEatenToday(int dogId) => Task.FromResult(eaten);
        public Task<IReadOnlyList<HealhtyPaws.Models.Dto.Bandit.BanditSlot>> GetBanditSlots(DateOnly day) => throw new NotSupportedException();
        public Task<double> GetPortionGrams(int dogId, int mealIndex) => throw new NotSupportedException();
    }
}
