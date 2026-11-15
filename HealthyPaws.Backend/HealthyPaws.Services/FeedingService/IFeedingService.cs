using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Database.Entity.Bandit;

namespace HealthyPaws.Services.FeedingService;

public interface IFeedingService
{
    public Task<double> CalculateEatenToday(int dogId);
    Task<IReadOnlyList<BanditSlot>> GetBanditSlots(DateOnly day);
    Task<double> GetPortionGrams(int dogId, int mealIndex);
}