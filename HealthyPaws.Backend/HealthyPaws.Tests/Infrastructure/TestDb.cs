using HealthyPaws.Database;
using HealthyPaws.Database.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthyPaws.Tests.Infrastructure;

/// <summary>
/// SQLite varijanta produkcijskog DbContexta. SQLite ne podržava SQL Server collation ni poređenje/sortiranje
/// DateTimeOffset kolona, pa se ovdje uklanja collation, a DateTimeOffset se čuva kao ticks (long).
/// </summary>
public sealed class SqliteHealthyPawsDbContext(DbContextOptions<HealthyPawsDbContext> options) : HealthyPawsDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
            property.SetCollation(null);
    }
}

/// <summary>Jedna in-memory SQLite baza po testu; svaki Create() vraća novi kontekst nad istom bazom.</summary>
public sealed class TestDb : IDisposable
{
    readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDb()
    {
        _connection.Open();
        using var context = Create();
        context.Database.EnsureCreated();
    }

    public HealthyPawsDbContext Create() => new SqliteHealthyPawsDbContext(
        new DbContextOptionsBuilder<HealthyPawsDbContext>().UseSqlite(_connection).Options);

    /// <summary>Pas je preduslov za FK u BanditDecisions/BanditStates/DogPlans.</summary>
    public async Task<Dog> AddDogAsync(string name = "Rex", decimal minWeight = 18, decimal maxWeight = 24)
    {
        await using var context = Create();
        var breed = new Breed { Name = "Test pasmina", MinWeight = minWeight, MaxWeight = maxWeight };
        context.Breeds.Add(breed);
        var dog = new Dog { Name = name, Breed = breed, DateOfBirth = DateTime.UtcNow.AddYears(-3), ActivityStatus = 1.2f, IsNeutered = true };
        context.Dogs.Add(dog);
        await context.SaveChangesAsync();
        return dog;
    }

    public void Dispose() => _connection.Dispose();
}

public sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
    public void Advance(TimeSpan by) => Now += by;
}
