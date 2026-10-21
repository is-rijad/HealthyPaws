using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Dog;
using HealhtyPaws.Models.Responses.Dog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Dog;

public class DogService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService) : BaseCrudService<Database.Entity.Dog, DogInsertRequest, DogInsertRequest, DogResponse, DogSearchObject>(dbContext, mapper, authorizationService), IDogService
{
    public async Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to)
    {
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == dogId))
            throw new NotFoundException("Nije pronađeno!");

        var dog = await dbContext.Dogs.AsNoTracking().FirstAsync(d => d.Id == dogId);
        var latestWeight = await dbContext.DogWeights
            .Where(w => w.DogId == dogId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => (double?)w.Weight)
            .FirstOrDefaultAsync();
        if (latestWeight == null)
            throw new BadRequestException("Pas nema zabilježenu težinu!");

        var periodStart = from?.ToUniversalTime() ?? DateTime.UtcNow.Date;
        var periodEnd = to?.ToUniversalTime() ?? periodStart.AddDays(1);

        var dailyBudget = Math.Round(70 * Math.Pow(latestWeight.Value, 0.75) * dog.ActivityStatus, 1);
        var treatCalories = await dbContext.TreatLogs
            .Where(l => l.DogId == dogId && l.GivenAt >= periodStart && l.GivenAt < periodEnd)
            .SumAsync(l => l.Calories);
        treatCalories = Math.Round(treatCalories, 1);

        return new CalorieBudgetResponse(dogId, dailyBudget, treatCalories, Math.Round(dailyBudget - treatCalories, 1));
    }

    public override async Task<IQueryable<Database.Entity.Dog>> ApplyFilter(IQueryable<Database.Entity.Dog> queryable, DogSearchObject searchObject)
    {
        queryable = await base.ApplyFilter(queryable, searchObject);
        if (!string.IsNullOrEmpty(searchObject.Contains))
        {
            queryable = queryable.Where(d => d.Name.Contains(searchObject.Contains, StringComparison.InvariantCultureIgnoreCase));
        }
        var userId = authorizationService.GetUserId();
        return queryable.Where(d => dbContext.UserDevices.Any(ud => ud.UserId == userId && ud.Device!.DogId == d.Id));
    }

    protected override async Task BeforeUpdate(int id, DogInsertRequest request, Database.Entity.Dog entity)
    {
        await base.BeforeUpdate(id, request, entity);
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == id))
            throw new NotFoundException("Nije pronađeno!");
    }

    protected override async Task PrepareForResponse(Database.Entity.Dog entity, HealthyPawsDbContext dbContext)
    {
        await base.PrepareForResponse(entity, dbContext);
        await dbContext.Entry(entity).Reference(d => d.Breed).LoadAsync();
    }

    protected override IQueryable<Database.Entity.Dog> ApplyOrdering(IQueryable<Database.Entity.Dog> queryable, DogSearchObject searchObject)
    {
        queryable = base.ApplyOrdering(queryable, searchObject);
        queryable = queryable.OrderByDescending(d => d.Id);
        return queryable;
    }
}