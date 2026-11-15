using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Database;
using HealthyPaws.Services.Bandit;
using HealthyPaws.Services.Exceptions;
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

    public async Task<IReadOnlyList<BanditSlot>> GetBanditSlots(DateOnly day)
    {
        var meals = await dbContext.DogMeals.AsNoTracking()
            .Where(m => dbContext.DogPlans.AsNoTracking().Any(plan => plan.DogId == m.DogId))
            .Select(m => new { m.DogId, m.Time, m.MealIndex })
            .ToListAsync();

        return meals
            .Select(m => new BanditSlot(m.DogId, m.MealIndex,
                day.ToDateTime(m.Time, DateTimeKind.Unspecified).AddMinutes(BanditOptions.BaselineOffsetMinutes)))
            .ToList();
    }

    public async Task<double> GetPortionGrams(int dogId, int mealIndex)
    {
        var plan = await dbContext.DogPlans.SingleOrDefaultAsync(it => it.DogId == dogId)
                   ?? throw new NotFoundException("Plan ishrane još nije kreiran!");
        var meals = await dbContext.DogMeals.CountAsync(it => it.DogId == dogId);
        if (meals == 0)
            throw new BadRequestException("Pas nema definisane obroke!");
        return plan.DailyKcal / meals; // TODO: Include treats
    }
}