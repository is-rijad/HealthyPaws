using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyPaws.Database;

public static class DatabaseConfiguration
{
    public static void AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<HealthyPawsDbContext>(options =>
            options.UseSqlServer(connectionString));
    }

    public static async Task MigrateDatabaseAsync(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthyPawsDbContext>();

        var databaseCreator = context.Database.GetService<IRelationalDatabaseCreator>();
        if (!await databaseCreator.ExistsAsync()) await databaseCreator.CreateAsync();

        await context.Database.MigrateAsync();
    }
}