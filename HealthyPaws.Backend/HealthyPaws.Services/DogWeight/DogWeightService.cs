using HealhtyPaws.Models.Requests.DogWeight;
using HealhtyPaws.Models.Responses.DogWeight;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.DogWeight;

public class DogWeightService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService)
    : BaseCrudService<Database.Entity.DogWeight, DogWeightInsertRequest, DogWeightUpdateRequest, DogWeightResponse, DogWeightSearchObject>(dbContext, mapper, authorizationService), IDogWeightService
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

    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    public override async Task<DogWeightResponse> InsertAsync(DogWeightInsertRequest request)
    {
        var response = await base.InsertAsync(request);

        var dogId = request.DogId!.Value;
        var lastWeights = await dbContext.DogWeights
            .Where(w => w.DogId == dogId)
            .OrderByDescending(w => w.CreatedAt)
            .Take(2).ToListAsync();
        var breed = (await dbContext.Dogs.Include(d => d.Breed).FirstAsync(d => d.Id == dogId)).Breed!;

        var latestWeight = lastWeights[0];

        return response;
    }

    protected override async Task BeforeInsert(DogWeightInsertRequest request)
    {
        await base.BeforeInsert(request);
        if (request.DogId == null)
            throw new BadRequestException("Pas je obavezan!");
        var dogId = request.DogId.Value;
        await EnsureOwnedAsync(dogId);

        if (request.CreatedAt == null)
            return;
        var createdAt = request.CreatedAt.Value.ToUniversalTime();
        if (createdAt > DateTime.UtcNow.Add(FutureTolerance))
            throw new BadRequestException("Datum mjerenja ne može biti u budućnosti!");

        var newestCreatedAt = await dbContext.DogWeights
            .Where(w => w.DogId == dogId)
            .MaxAsync(w => (DateTime?)w.CreatedAt);
        if (newestCreatedAt != null && createdAt < newestCreatedAt)
            throw new BadRequestException("Datum mjerenja ne može biti stariji od najnovijeg mjerenja!");
    }

    protected override Database.Entity.DogWeight MapToInsertEntity(DogWeightInsertRequest request)
    {
        var entity = base.MapToInsertEntity(request);
        entity.CreatedAt = request.CreatedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        return entity;
    }

    protected override async Task BeforeDelete(int id, Database.Entity.DogWeight entity)
    {
        await base.BeforeDelete(id, entity);
        await EnsureOwnedAsync(entity.DogId);
    }

    private async Task EnsureOwnedAsync(int dogId)
    {
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == dogId))
            throw new NotFoundException("Nije pronađeno!");
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
