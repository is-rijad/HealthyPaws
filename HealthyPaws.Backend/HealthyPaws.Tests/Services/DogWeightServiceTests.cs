using CalorieAgent;
using HealhtyPaws.Models.Requests.DogWeight;
using HealhtyPaws.Models.Responses.Calorie;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database.Entity;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.DogWeight;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.Services;

public class DogWeightServiceTests : IDisposable
{
    readonly TestDb _db = new();
    readonly FakeAgent _agent = new();
    public void Dispose() => _db.Dispose();

    DogWeightService Sut(int? userId)
    {
        var ctx = _db.Create();
        return new DogWeightService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId), _agent);
    }

    async Task<(Dog Dog, User Owner)> DogWithOwner()
    {
        var dog = await _db.AddDogAsync();
        return (dog, await _db.AddOwnerAsync(dog));
    }

    static DogWeightInsertRequest Req(int? dogId, double weight = 20, DateTime? at = null) =>
        new() { DogId = dogId, Bcs = 5, Weight = weight, Calorie = 350, CreatedAt = at };

    [Fact]
    public async Task Insert_saves_weight_and_calls_agent_once_with_the_saved_dog_and_weight()
    {
        var (dog, owner) = await DogWithOwner();
        var response = await Sut(owner.Id).InsertAsync(Req(dog.Id, 21.5));

        Assert.Equal(21.5, response.Weight);
        Assert.Equal(1, await _db.Create().DogWeights.CountAsync());
        var (calledDog, calledWeight) = Assert.Single(_agent.Ticks);
        Assert.Equal((dog.Id, 21.5), (calledDog, calledWeight));
    }

    [Fact]
    public async Task Insert_requires_dog()
    {
        var (_, owner) = await DogWithOwner();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Sut(owner.Id).InsertAsync(Req(null)));
        Assert.Contains("Pas je obavezan", ex.Message);
    }

    [Fact]
    public async Task Insert_for_someone_elses_dog_is_not_found()
    {
        var (dog, _) = await DogWithOwner();
        var stranger = await _db.AddOwnerAsync(await _db.AddDogAsync("Tuđi"), "stranger@test.ba");
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).InsertAsync(Req(dog.Id)));
        Assert.Empty(_agent.Ticks);
    }

    [Fact]
    public async Task Insert_in_the_future_is_rejected()
    {
        var (dog, owner) = await DogWithOwner();
        await Assert.ThrowsAsync<BadRequestException>(() => Sut(owner.Id).InsertAsync(Req(dog.Id, at: DateTime.UtcNow.AddHours(1))));
    }

    [Fact]
    public async Task Insert_within_five_minute_future_tolerance_is_accepted()
    {
        var (dog, owner) = await DogWithOwner();
        await Sut(owner.Id).InsertAsync(Req(dog.Id, at: DateTime.UtcNow.AddMinutes(4)));
    }

    [Fact]
    public async Task Insert_older_than_newest_measurement_is_rejected()
    {
        var (dog, owner) = await DogWithOwner();
        await Sut(owner.Id).InsertAsync(Req(dog.Id, at: DateTime.UtcNow.AddDays(-1)));
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Sut(owner.Id).InsertAsync(Req(dog.Id, at: DateTime.UtcNow.AddDays(-3))));
        Assert.Contains("stariji", ex.Message);
    }

    [Fact]
    public async Task Insert_without_date_uses_now()
    {
        var (dog, owner) = await DogWithOwner();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var saved = await Sut(owner.Id).InsertAsync(Req(dog.Id));
        Assert.InRange(saved.CreatedAt.ToUniversalTime(), before, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task When_agent_rejects_the_measurement_the_weight_is_not_saved()
    {
        // Agent baca BadRequest samo za nevaljan ulaz (has_error); tada se vaganje ne sprema.
        var (dog, owner) = await DogWithOwner();
        _agent.Failure = new BadRequestException("Težina je u zdravom rasponu, plan ostaje isti.");
        await Assert.ThrowsAsync<BadRequestException>(() => Sut(owner.Id).InsertAsync(Req(dog.Id)));
        Assert.Equal(0, await _db.Create().DogWeights.CountAsync());
    }

    [Fact]
    public async Task Get_returns_only_own_dogs_weights_newest_first()
    {
        var (dog, owner) = await DogWithOwner();
        var (otherDog, _) = await DogWithOwner();
        foreach (var (d, kg, days) in new[] { (dog.Id, 20.0, -3), (dog.Id, 21.0, -2), (otherDog.Id, 99.0, -1) })
            await Seed(d, kg, days);

        var page = await Sut(owner.Id).GetAsync(new DogWeightSearchObject());
        Assert.Equal([21.0, 20.0], page.Items.Select(i => i.Weight));
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Get_filters_and_orders()
    {
        var (dog, owner) = await DogWithOwner();
        await Seed(dog.Id, 20, -4, bcs: 5);
        await Seed(dog.Id, 25, -3, bcs: 7);
        await Seed(dog.Id, 30, -2, bcs: 9);

        var heavy = await Sut(owner.Id).GetAsync(new DogWeightSearchObject { MinWeight = 25 });
        Assert.Equal([30.0, 25.0], heavy.Items.Select(i => i.Weight));

        var bcs = await Sut(owner.Id).GetAsync(new DogWeightSearchObject { Bcs = 7 });
        Assert.Equal([25.0], bcs.Items.Select(i => i.Weight));

        var byWeightAsc = await Sut(owner.Id).GetAsync(new DogWeightSearchObject { OrderBy = "weight", Descending = false });
        Assert.Equal([20.0, 25.0, 30.0], byWeightAsc.Items.Select(i => i.Weight));

        var window = await Sut(owner.Id).GetAsync(new DogWeightSearchObject
            { From = DateTime.UtcNow.AddDays(-3.5), To = DateTime.UtcNow.AddDays(-2.5) });
        Assert.Equal([25.0], window.Items.Select(i => i.Weight));
    }

    [Fact]
    public async Task Paging_reports_has_more()
    {
        var (dog, owner) = await DogWithOwner();
        for (var i = 0; i < 5; i++) await Seed(dog.Id, 20 + i, -10 + i);
        var first = await Sut(owner.Id).GetAsync(new DogWeightSearchObject { PageSize = 2 });
        var last = await Sut(owner.Id).GetAsync(new DogWeightSearchObject { PageSize = 2, Page = 3 });
        Assert.Equal((2, true), (first.Items.Count, first.HasMore));
        Assert.Equal((1, false), (last.Items.Count, last.HasMore));
    }

    [Fact]
    public async Task Delete_requires_ownership()
    {
        var (dog, owner) = await DogWithOwner();
        var id = await Seed(dog.Id, 20, -1);
        var stranger = await _db.AddOwnerAsync(await _db.AddDogAsync("Tuđi"), "s@test.ba");
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).DeleteAsync(id));
        await Sut(owner.Id).DeleteAsync(id);
        Assert.Equal(0, await _db.Create().DogWeights.CountAsync());
    }

    [Fact]
    public async Task Delete_unknown_id_is_not_found()
    {
        var (_, owner) = await DogWithOwner();
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(owner.Id).DeleteAsync(12345));
    }

    [Fact]
    public async Task Latest_weight()
    {
        var (dog, owner) = await DogWithOwner();
        await Assert.ThrowsAsync<BadRequestException>(() => Sut(owner.Id).GetLatestDogWeightAsync(dog.Id));
        await Seed(dog.Id, 20, -5);
        await Seed(dog.Id, 22, -1);
        await Seed(dog.Id, 19, -9);
        Assert.Equal(22, await Sut(owner.Id).GetLatestDogWeightAsync(dog.Id));
    }

    async Task<int> Seed(int dogId, double kg, int daysAgo, int bcs = 5)
    {
        await using var ctx = _db.Create();
        var weight = new DogWeight { DogId = dogId, Weight = kg, Bcs = bcs, Calorie = 350, CreatedAt = DateTime.UtcNow.AddDays(daysAgo) };
        ctx.DogWeights.Add(weight);
        await ctx.SaveChangesAsync();
        return weight.Id;
    }

    sealed class FakeAgent : ICalorieAgentService
    {
        public List<(int DogId, double Weight)> Ticks { get; } = [];
        public Exception? Failure { get; set; }

        public Task<CalorieAgentResponse> TickAsync(Dog dog, DogWeight weight)
        {
            if (Failure is not null) throw Failure;
            Ticks.Add((dog.Id, weight.Weight));
            return Task.FromResult(new CalorieAgentResponse());
        }

        public Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to) => throw new NotSupportedException();
    }
}
