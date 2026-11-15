using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.Dog;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.Services;

/// <summary>
/// Provjerava da se upiti iz servisa mogu prevesti u SQL za PRODUKCIJSKI provider (SQL Server).
/// Ne treba živa baza: ToQueryString samo kompajlira upit.
/// </summary>
public class QueryTranslationTests
{
    static HealthyPawsDbContext SqlServer() => new(
        new DbContextOptionsBuilder<HealthyPawsDbContext>().UseSqlServer("Server=localhost;Database=none;User Id=x;Password=x;TrustServerCertificate=true").Options);

    [Fact]
    public async Task Dog_name_search_from_DogService_translates_to_sql_server()
    {
        await using var ctx = SqlServer();
        var service = new DogService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId: 1), null!);
        var query = await service.ApplyFilter(ctx.Dogs, new DogSearchObject { Contains = "rex" });
        Assert.Contains("LIKE", query.ToQueryString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Feeding_records_range_query_from_DeviceWeightReader_translates_to_sql_server()
    {
        using var ctx = SqlServer();
        DateTimeOffset from = DateTimeOffset.UtcNow.AddHours(-1), to = DateTimeOffset.UtcNow;
        // identično izrazu iz DeviceWeightReader.Read
        var query = ctx.FeedingRecords.Where(it => it.DogId == 1 && it.EatenAt >= from && it.EatenAt <= to);
        Assert.Contains("WHERE", query.ToQueryString());
    }
}
