using HealhtyPaws.Models.Responses.DogWeight;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.DogWeight;

public class DogWeightService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService) : BaseService<DogWeightResponse, Database.Entity.DogWeight, DogWeightSearchObject>(dbContext, mapper), IDogWeightService
{
    public override async Task<IQueryable<Database.Entity.DogWeight>> ApplyFilter(IQueryable<Database.Entity.DogWeight> queryable, DogWeightSearchObject searchObject)
    {
        queryable = await base.ApplyFilter(queryable, searchObject);
        var userId = authorizationService.GetUserId();
        queryable = queryable.Where(w => dbContext.UserDevices.Any(ud => ud.UserId == userId && ud.Device!.DogId == w.DogId));

        if (searchObject.DogId != null)
            queryable = queryable.Where(w => w.DogId == searchObject.DogId);
        if (searchObject.From != null)
        {
            var from = searchObject.From.Value.ToUniversalTime();
            queryable = queryable.Where(w => w.CreatedAt >= from);
        }
        if (searchObject.To != null)
        {
            var to = searchObject.To.Value.ToUniversalTime();
            queryable = queryable.Where(w => w.CreatedAt < to);
        }
        if (searchObject.MinWeight != null)
            queryable = queryable.Where(w => w.Weight >= searchObject.MinWeight);
        if (searchObject.MaxWeight != null)
            queryable = queryable.Where(w => w.Weight <= searchObject.MaxWeight);
        if (searchObject.Bcs != null)
            queryable = queryable.Where(w => w.Bcs == searchObject.Bcs);

        return queryable;
    }

    protected override IQueryable<Database.Entity.DogWeight> ApplyOrdering(IQueryable<Database.Entity.DogWeight> queryable, DogWeightSearchObject searchObject)
    {
        var descending = searchObject.Descending;
        var ordered = searchObject.OrderBy?.ToLowerInvariant() switch
        {
            "weight" => descending ? queryable.OrderByDescending(w => w.Weight) : queryable.OrderBy(w => w.Weight),
            "bcs" => descending ? queryable.OrderByDescending(w => w.Bcs) : queryable.OrderBy(w => w.Bcs),
            _ => descending ? queryable.OrderByDescending(w => w.CreatedAt) : queryable.OrderBy(w => w.CreatedAt)
        };
        return descending ? ordered.ThenByDescending(w => w.Id) : ordered.ThenBy(w => w.Id);
    }

    public async Task<double> GetLatestDogWeightAsync(int dogId)
    {
        var latestWeight = await dbContext.DogWeights
            .Where(w => w.DogId == dogId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => (double?)w.Weight)
            .FirstOrDefaultAsync();
        if (latestWeight == null)
            throw new BadRequestException("Pas nema zabilježenu težinu!");
        return latestWeight.Value;
    }
}
