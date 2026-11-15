using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.AgentInsights;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Tests.Infrastructure;

namespace HealthyPaws.Tests.Services;

public class AgentAlertServiceTests : IDisposable
{
    readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    AgentAlertService Sut(int? userId)
    {
        var ctx = _db.Create();
        return new AgentAlertService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId));
    }

    async Task<int> AddAlertAsync(int dogId, string reason)
    {
        var ctx = _db.Create();
        var alert = new Alert
        {
            DogId = dogId, Reason = reason, Explanation = "x",
            FirstSeenAt = DateTimeOffset.UtcNow, LastSeenAt = DateTimeOffset.UtcNow
        };
        ctx.Alerts.Add(alert);
        await ctx.SaveChangesAsync();
        return (int)alert.Id;
    }

    [Theory]
    [InlineData("UNEXPLAINED_WEIGHT_LOSS", "Veterinar")]
    [InlineData("INTAKE_IMPLAUSIBLE", "ProvjeraPodataka")]
    [InlineData("UNSUPPORTED_LIFE_STAGE", "RucnaProcjena")]
    [InlineData("SOMETHING_NEW", "Veterinar")]
    public async Task Alerts_carry_category_and_recommendation(string reason, string category)
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        await AddAlertAsync(dog.Id, reason);

        var result = await Sut(owner.Id).GetAsync(new AgentAlertSearchObject());

        var alert = Assert.Single(result.Items);
        Assert.Equal(category, alert.Category);
        Assert.False(string.IsNullOrWhiteSpace(alert.RecommendedAction));
    }

    [Fact]
    public async Task Alerts_of_other_users_dogs_are_hidden()
    {
        var dog = await _db.AddDogAsync();
        await AddAlertAsync(dog.Id, "REDUCED_APPETITE");
        var stranger = await _db.AddOwnerAsync(await _db.AddDogAsync("Tuđi"), "stranger@test.ba");

        Assert.Empty((await Sut(stranger.Id).GetAsync(new AgentAlertSearchObject())).Items);
    }

    [Fact]
    public async Task Resolve_marks_alert_resolved_by_user_and_rejects_strangers()
    {
        var dog = await _db.AddDogAsync();
        var owner = await _db.AddOwnerAsync(dog);
        var stranger = await _db.AddOwnerAsync(await _db.AddDogAsync("Tuđi"), "stranger@test.ba");
        var id = await AddAlertAsync(dog.Id, "REDUCED_APPETITE");

        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).ResolveAsync(id));
        var resolved = await Sut(owner.Id).ResolveAsync(id);

        Assert.NotNull(resolved.ResolvedAt);
        Assert.Equal("User", resolved.ResolvedBy);
    }
}
