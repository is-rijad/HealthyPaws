using System.Security.Claims;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.Mapster;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HealthyPaws.Tests.Infrastructure;

public static class TestServices
{
    /// <summary>Isto što radi Program.cs (IgnoreNullValues + scan), ali na lokalnoj konfiguraciji bez globalnog stanja.</summary>
    public static IMapper Mapper()
    {
        var config = new TypeAdapterConfig();
        config.Default.IgnoreNullValues(true);
        config.Scan(typeof(MapsterConfiguration).Assembly);
        return new Mapper(config);
    }

    public static IConfiguration Jwt(string key = "unit-test-signing-key-which-is-long-enough-123456", string issuer = "healthy-paws-tests") =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = key, ["Jwt:Issuer"] = issuer, ["Jwt:ExpirationInMinutes"] = "15", ["Jwt:RefreshExpirationInMinutes"] = "60",
        }).Build();

    /// <summary>AuthorizationService kojem GetUserId() vraća zadatog korisnika (claim "sub", kao u produkciji).</summary>
    public static AuthorizationService Auth(HealthyPawsDbContext dbContext, int? userId)
    {
        var http = new DefaultHttpContext();
        if (userId is { } id)
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "test"));
        var tokenManager = new TokenManager(Jwt(), new HttpContextAccessor { HttpContext = http }, dbContext, Mapper());
        return new AuthorizationService(dbContext, tokenManager);
    }

    /// <summary>Korisnik + uređaj + veza korisnik-uređaj-pas (vlasništvo ide preko uređaja).</summary>
    public static async Task<User> AddOwnerAsync(this TestDb db, Dog dog, string email = "owner@test.ba")
    {
        await using var ctx = db.Create();
        var user = new User { Email = email, PasswordHash = "h", PasswordSalt = "s" };
        var device = new Device { DogId = dog.Id, MacAddress = $"AA:BB:CC:DD:EE:{Random.Shared.Next(10, 99)}" };
        ctx.Users.Add(user);
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync();
        ctx.UserDevices.Add(new UserDevice { UserId = user.Id, DeviceId = device.Id });
        await ctx.SaveChangesAsync();
        return user;
    }
}
