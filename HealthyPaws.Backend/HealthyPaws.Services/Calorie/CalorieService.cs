using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Dog;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Services.TreatLog;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Calorie;

public class CalorieService(HealthyPawsDbContext dbContext, AuthorizationService authorizationService, ITreatLogService treatLogService) : ICalorieService
{
    public async Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to)
    {
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == dogId))
            throw new NotFoundException("Nije pronađeno!");

        var dog = await dbContext.Dogs.AsNoTracking().FirstAsync(d => d.Id == dogId);
        var treatCalories = await treatLogService.GetTreatCaloriesAsync(dogId, from, to);
        
        return new CalorieBudgetResponse(dogId, dog.DailyCalorieBudget, treatCalories, Math.Round(dog.DailyCalorieBudget - treatCalories, 1));
    }

    public double CalculateDailyBudget(DeviceInsertRequest deviceInsertRequest)
    {
        return Math.Round(70 * Math.Pow(deviceInsertRequest.DogWeight!.Weight, 0.75) * deviceInsertRequest.Dog!.ActivityStatus, 1);
    }
}
