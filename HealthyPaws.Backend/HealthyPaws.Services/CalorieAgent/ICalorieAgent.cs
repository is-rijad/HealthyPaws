using CalorieAgent;
using HealhtyPaws.Models.Responses.Calorie;
using HealhtyPaws.Models.Responses.Dog;

namespace HealthyPaws.Services.CalorieAgent;

public interface ICalorieAgentService
{
    public Task<CalorieAgentResponse> TickAsync(Database.Entity.Dog dog, Database.Entity.DogWeight weight);
    public Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to);

}