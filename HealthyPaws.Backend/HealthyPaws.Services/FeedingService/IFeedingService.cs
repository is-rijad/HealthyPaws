namespace HealthyPaws.Services.FeedingService;

public interface IFeedingService
{
    public Task<double> CalculateEatenToday(int dogId);
}