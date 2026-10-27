using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Dog;

namespace HealthyPaws.Services.Calorie;

public interface ICalorieService
{
    Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to);
    public double CalculateDailyBudget(DeviceInsertRequest deviceInsertRequest);
}
