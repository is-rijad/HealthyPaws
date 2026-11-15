using CalorieAgent;
using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Requests.Dog;
using HealhtyPaws.Models.Requests.DogWeight;
using HealhtyPaws.Models.Responses.Calorie;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database.Entity;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.Device;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.DeviceTests;

public class DeviceServiceTests : IDisposable
{
    const string Mac = "AA:BB:CC:DD:EE:01";
    readonly TestDb _db = new();
    readonly FakeAgent _agent = new();
    public void Dispose() => _db.Dispose();

    DeviceService Sut(int userId)
    {
        var ctx = _db.Create();
        return new DeviceService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId), _agent);
    }

    async Task<User> NewUser(string email = "u@test.ba")
    {
        await using var ctx = _db.Create();
        var user = new User { Email = email, PasswordHash = "h", PasswordSalt = "s" };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    async Task<int> BreedId() => (await _db.Create().Breeds.FirstAsync()).Id;

    async Task<DeviceInsertRequest> FullRequest(string mac = Mac, List<TimeOnly>? meals = null) => new()
    {
        MacAddress = mac,
        Dog = new DogInsertRequest("Rex", await BreedId(), DateTime.UtcNow.AddYears(-3), 0, 1.2f, true),
        DogWeight = new DogWeightInsertRequest { Bcs = 5, Weight = 20, Calorie = 350 },
        MealTimes = meals ?? [new TimeOnly(18, 0), new TimeOnly(8, 0)],
    };

    // ---------------------------------------------------------------- registracija uređaja s novim psom

    [Fact]
    public async Task Registering_a_new_device_creates_dog_weight_meals_device_and_ownership()
    {
        var user = await NewUser();
        var response = await Sut(user.Id).InsertAsync(await FullRequest());

        Assert.Equal(Mac, response.MacAddress);
        Assert.True(response.IsConnected);
        await using var ctx = _db.Create();
        var dog = await ctx.Dogs.SingleAsync();
        Assert.Equal("Rex", dog.Name);
        Assert.Equal(dog.Id, response.DogId);
        var weight = await ctx.DogWeights.SingleAsync();
        Assert.Equal((dog.Id, 20d, 350d), (weight.DogId, weight.Weight, weight.Calorie));
        Assert.InRange(weight.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        var link = await ctx.UserDevices.SingleAsync();
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(1, await ctx.Devices.CountAsync());
    }

    [Fact]
    public async Task Meal_times_are_sorted_and_indexed_from_zero()
    {
        var user = await NewUser();
        await Sut(user.Id).InsertAsync(await FullRequest(meals: [new(20, 0), new(7, 30), new(13, 0)]));
        var meals = await _db.Create().DogMeals.OrderBy(m => m.MealIndex).ToListAsync();
        Assert.Equal([new TimeOnly(7, 30), new TimeOnly(13, 0), new TimeOnly(20, 0)], meals.Select(m => m.Time));
        Assert.Equal([0, 1, 2], meals.Select(m => (int)m.MealIndex));
    }

    [Fact]
    public async Task Agent_receives_the_new_dog_and_its_first_weight()
    {
        var user = await NewUser();
        await Sut(user.Id).InsertAsync(await FullRequest());
        var (dogName, weight, dogId) = Assert.Single(_agent.Ticks);
        Assert.Equal(("Rex", 20d), (dogName, weight));
        Assert.True(dogId > 0);
    }

    [Fact]
    public async Task If_the_agent_requests_remeasure_of_the_first_weight_nothing_is_created()
    {
        var user = await NewUser();
        _agent.Next = new CalorieAgentResponse
        {
            Action = global::CalorieAgent.Action.RequestRemeasure,
            Explanation = "Ponovite vaganje"
        };
        var ex = await Assert.ThrowsAsync<BadRequestException>(async () => await Sut(user.Id).InsertAsync(await FullRequest()));
        Assert.Equal("Ponovite vaganje", ex.Message);
        await using var ctx = _db.Create();
        Assert.Equal((0, 0, 0, 0, 0), (await ctx.Dogs.CountAsync(), await ctx.Devices.CountAsync(), await ctx.DogMeals.CountAsync(), await ctx.UserDevices.CountAsync(), await ctx.DogWeights.CountAsync()));
    }

    [Fact]
    public async Task If_the_agent_rejects_the_first_weight_nothing_is_created()
    {
        var user = await NewUser();
        _agent.Failure = new BadRequestException("Unesena težina nije valjana");
        await Assert.ThrowsAsync<BadRequestException>(async () => await Sut(user.Id).InsertAsync(await FullRequest()));
        await using var ctx = _db.Create();
        Assert.Equal((0, 0, 0, 0), (await ctx.Dogs.CountAsync(), await ctx.Devices.CountAsync(), await ctx.DogMeals.CountAsync(), await ctx.UserDevices.CountAsync()));
    }

    [Fact]
    public async Task Same_user_cannot_register_the_same_mac_twice()
    {
        var user = await NewUser();
        await Sut(user.Id).InsertAsync(await FullRequest());
        var ex = await Assert.ThrowsAsync<BadRequestException>(async () => await Sut(user.Id).InsertAsync(await FullRequest()));
        Assert.Contains("Već posjedujete", ex.Message);
        Assert.Equal(1, await _db.Create().Devices.CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task Meal_count_must_be_between_one_and_four(int count)
    {
        var user = await NewUser();
        var meals = Enumerable.Range(0, count).Select(i => new TimeOnly(6 + i * 3, 0)).ToList();
        var ex = await Assert.ThrowsAsync<BadRequestException>(async () => await Sut(user.Id).InsertAsync(await FullRequest(meals: meals)));
        Assert.Contains("1 do 4", ex.Message);
    }

    [Fact]
    public async Task Four_meals_is_the_maximum_and_matches_the_bandit_meal_index_range()
    {
        var user = await NewUser();
        await Sut(user.Id).InsertAsync(await FullRequest(meals: [new(6, 0), new(10, 0), new(14, 0), new(18, 0)]));
        Assert.Equal(3, await _db.Create().DogMeals.MaxAsync(m => (int)m.MealIndex));   // LinUCB: mealIndex 0..3
    }

    [Fact]
    public async Task Duplicate_meal_times_are_rejected()
    {
        var user = await NewUser();
        var ex = await Assert.ThrowsAsync<BadRequestException>(async () => await Sut(user.Id).InsertAsync(await FullRequest(meals: [new(8, 0), new(8, 0)])));
        Assert.Contains("različita", ex.Message);
    }

    // ---------------------------------------------------------------- već poznat uređaj

    [Fact]
    public async Task Second_user_joining_an_existing_device_shares_the_dog_without_a_new_one()
    {
        var first = await NewUser("a@test.ba");
        var second = await NewUser("b@test.ba");
        await Sut(first.Id).InsertAsync(await FullRequest());

        await Sut(second.Id).InsertAsync(new DeviceInsertRequest { MacAddress = Mac });

        await using var ctx = _db.Create();
        Assert.Equal(1, await ctx.Dogs.CountAsync());
        Assert.Equal([first.Id, second.Id], (await ctx.UserDevices.OrderBy(u => u.UserId).ToListAsync()).Select(u => u.UserId));
        Assert.Single(_agent.Ticks);   // agent se ne zove ponovo
    }

    [Fact]
    public async Task Unknown_mac_without_dog_data_is_a_bad_request_not_a_server_error()
    {
        var user = await NewUser();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Sut(user.Id).InsertAsync(new DeviceInsertRequest { MacAddress = "11:22:33:44:55:66" }));
        Assert.Contains("podatke o psu", ex.Message);
        Assert.Equal(0, await _db.Create().Devices.CountAsync());
    }

    // ---------------------------------------------------------------- listanje / izmjena

    [Fact]
    public async Task List_returns_only_own_devices_with_dog_breed_and_latest_weight()
    {
        var mine = await NewUser("a@test.ba");
        var other = await NewUser("b@test.ba");
        await Sut(mine.Id).InsertAsync(await FullRequest());
        await Sut(other.Id).InsertAsync(await FullRequest("AA:BB:CC:DD:EE:02"));

        var page = await Sut(mine.Id).GetAsync(new DeviceSearchObject());

        var device = Assert.Single(page.Items);
        Assert.Equal(Mac, device.MacAddress);
        Assert.Equal("Rex", device.Dog!.Name);
        Assert.NotNull(device.Dog.Breed);
    }

    [Fact]
    public async Task Update_requires_device_ownership()
    {
        var owner = await NewUser("a@test.ba");
        var stranger = await NewUser("b@test.ba");
        var created = await Sut(owner.Id).InsertAsync(await FullRequest());
        await Assert.ThrowsAsync<NotFoundException>(() => Sut(stranger.Id).UpdateAsync(created.Id, new DeviceUpdateRequest { DogId = created.DogId }));
    }

    [Fact]
    public async Task Update_can_only_assign_a_dog_the_user_owns()
    {
        var owner = await NewUser("a@test.ba");
        var created = await Sut(owner.Id).InsertAsync(await FullRequest());
        var foreignDog = await _db.AddDogAsync("Tuđi");
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Sut(owner.Id).UpdateAsync(created.Id, new DeviceUpdateRequest { DogId = foreignDog.Id }));
        Assert.Contains("pas nije pronađen", ex.Message);
    }

    [Fact]
    public async Task Update_to_own_dog_succeeds()
    {
        var owner = await NewUser();
        var first = await Sut(owner.Id).InsertAsync(await FullRequest());
        var second = await Sut(owner.Id).InsertAsync(await FullRequest("AA:BB:CC:DD:EE:03"));
        var updated = await Sut(owner.Id).UpdateAsync(first.Id, new DeviceUpdateRequest { DogId = second.DogId });
        Assert.Equal(second.DogId, updated.DogId);
    }

    [Fact]
    public async Task Device_mac_lookup_by_dog()
    {
        var owner = await NewUser();
        var created = await Sut(owner.Id).InsertAsync(await FullRequest());
        Assert.Equal(Mac, await Sut(owner.Id).GetDeviceMac(created.DogId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut(owner.Id).GetDeviceMac(9999));
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF", true)]
    [InlineData("aa:bb:cc:dd:ee:ff", false)]    // regex traži velika slova
    [InlineData("AA-BB-CC-DD-EE-FF", false)]
    [InlineData("AA:BB:CC:DD:EE", false)]
    public void Mac_address_format_is_validated(string mac, bool valid)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var request = new DeviceInsertRequest { MacAddress = mac };
        var ok = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request, new System.ComponentModel.DataAnnotations.ValidationContext(request), results, validateAllProperties: true);
        Assert.Equal(valid, ok);
    }

    sealed class FakeAgent : ICalorieAgentService
    {
        public List<(string Name, double Weight, int DogId)> Ticks { get; } = [];
        public Exception? Failure { get; set; }
        public CalorieAgentResponse? Next { get; set; }
        public Task<CalorieAgentResponse> TickAsync(Dog dog, DogWeight weight)
        {
            if (Failure is not null) throw Failure;
            Ticks.Add((dog.Name, weight.Weight, dog.Id));
            return Task.FromResult(Next ?? new CalorieAgentResponse());
        }
        public Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to) => throw new NotSupportedException();
    }
}
