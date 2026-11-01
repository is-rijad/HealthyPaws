using CalorieAgent;

namespace HealthyPaws.Services.CalorieAgent;

public interface ICalorieAgentService
{
    public Task<CalorieAgentResponse> TickAsync(CalorieAgentRequest request);
}