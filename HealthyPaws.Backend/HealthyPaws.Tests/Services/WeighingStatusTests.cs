using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.Dog;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Tests.Infrastructure;

namespace HealthyPaws.Tests.Services;

public class WeighingStatusTests : IDisposable
{
    readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    DogService Sut(int? userId)
    {
        var ctx = _db.Create();
        return new DogService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId), null!);
    }

    async Task AddPlan(int dogId, DateTime nextMeasureAt)
    {
        var ctx = _db.Create();
        ctx.DogPlans.Add(new DogPlan { DogId = dogId, NextMeasureAt = nextMeasureAt });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Dog_without_plan_is_due_with_no_date()
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        var status = await Sut(owner.Id).GetWeighingStatusAsync(dog.Id);
        Assert.True(status.IsDue);
        Assert.Null(status.NextMeasureAt);
    }

    [Fact]
    public async Task Future_date_is_not_due()
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        await AddPlan(dog.Id, DateTime.UtcNow.AddDays(3));
        var status = await Sut(owner.Id).GetWeighingStatusAsync(dog.Id);
        Assert.False(status.IsDue);
        Assert.Equal(0, status.DaysOverdue);
    }

    [Fact]
    public async Task Past_date_is_due_with_full_days_overdue()
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        await AddPlan(dog.Id, DateTime.UtcNow.AddDays(-2).AddHours(-1));
        var status = await Sut(owner.Id).GetWeighingStatusAsync(dog.Id);
        Assert.True(status.IsDue);
        Assert.Equal(2, status.DaysOverdue);
    }

    [Fact]
    public async Task Someone_elses_dog_is_not_found()
    {
        var dog = await _db.AddDogAsync();
        var stranger = await _db.AddOwnerAsync(await _db.AddDogAsync("Tuđi"), "stranger@test.ba");
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).GetWeighingStatusAsync(dog.Id));
    }
}
