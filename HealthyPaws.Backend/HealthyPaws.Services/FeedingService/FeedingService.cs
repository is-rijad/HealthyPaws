
using HealthyPaws.Database;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.FeedingService;

public class FeedingService(HealthyPawsDbContext dbContext) : IFeedingService
{
    public async Task<double> CalculateEatenToday(int dogId)
    {
        var eatenToday = await dbContext.FeedingRecords.Where(x => x.DogId == dogId && x.EatenAt.Date == DateTime.UtcNow.Date)
            .SumAsync(x => x.EatenG);
        
        return Math.Round(eatenToday, 1);
    }
}