using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Requests.DogWeight;
using HealhtyPaws.Models.Responses.Device;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Device;

public class DeviceService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService) : BaseCrudService<Database.Entity.Device, DeviceInsertRequest, DeviceUpdateRequest, DeviceResponse, DeviceSearchObject>(dbContext, mapper, authorizationService), IDeviceService
{
    public override async Task<DeviceResponse> InsertAsync(DeviceInsertRequest request) {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await BeforeInsert(request);

        var dog = (await dbContext.Devices.Include(d => d.Dog).FirstOrDefaultAsync(d => d.MacAddress == request.MacAddress))?.Dog;
        if (request.Dog != null && request.DogWeight != null) {
            var dogEntityEntry = await dbContext.Dogs.AddAsync(mapper.Map<Database.Entity.Dog>(request.Dog));
            await dbContext.SaveChangesAsync();
            dog = dogEntityEntry.Entity;
            var dogWeightInsertRequest = mapper.Map<Database.Entity.DogWeight>(request.DogWeight);
            dogWeightInsertRequest.DogId = dog.Id;
            dogWeightInsertRequest.CreatedAt = DateTime.UtcNow;
            await dbContext.DogWeights.AddAsync(dogWeightInsertRequest);
            await dbContext.DogMeals.AddRangeAsync(request.MealTimes!.Select(t => new Database.Entity.DogMeal
            {
                DogId = dog.Id,
                Time = t
            }));
            await dbContext.SaveChangesAsync();
        }

        var entityEntry = await dbContext.Devices.AddAsync(new Database.Entity.Device()
        {
            DogId = dog!.Id,
            MacAddress = request.MacAddress,
            IsConnected = true
        });
        await AfterInsert(entityEntry.Entity, request, dbContext);
        await dbContext.SaveChangesAsync();


        await dbContext.UserDevices.AddAsync(new Database.Entity.UserDevice()
        {
            DeviceId = entityEntry.Entity.Id,
            UserId = authorizationService.GetUserId()
        });

        await dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        await PrepareForResponse(entityEntry.Entity, dbContext);
        return MapToResponseModel<DeviceResponse>(entityEntry.Entity, mapper);
    }

    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    public async Task<DeviceResponse> AddWeightAsync(int deviceId, DogWeightInsertRequest request) {
        var userId = authorizationService.GetUserId();
        var device = await dbContext.Devices
            .Include(d => d.Dog).ThenInclude(d => d!.Breed)
            .FirstOrDefaultAsync(d => d.Id == deviceId
                                      && dbContext.UserDevices.Any(ud => ud.DeviceId == d.Id && ud.UserId == userId));
        if (device == null)
            throw new NotFoundException("Nije pronađeno!");

        var createdAt = DateTime.UtcNow;
        if (request.CreatedAt != null) {
            createdAt = request.CreatedAt.Value.ToUniversalTime();
            if (createdAt > DateTime.UtcNow.Add(FutureTolerance))
                throw new BadRequestException("Datum mjerenja ne može biti u budućnosti!");

            var newestCreatedAt = await dbContext.DogWeights
                .Where(w => w.DogId == device.DogId)
                .MaxAsync(w => (DateTime?)w.CreatedAt);
            if (newestCreatedAt != null && createdAt < newestCreatedAt)
                throw new BadRequestException("Datum mjerenja ne može biti stariji od najnovijeg mjerenja!");
        }

        var dogWeight = mapper.Map<Database.Entity.DogWeight>(request);
        dogWeight.DogId = device.DogId;
        dogWeight.CreatedAt = createdAt;
        await dbContext.DogWeights.AddAsync(dogWeight);
        await dbContext.SaveChangesAsync();

        var latestWeight = await dbContext.DogWeights
            .Where(w => w.DogId == device.DogId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstAsync();

        var response = MapToResponseModel<DeviceResponse>(device, mapper);
        response.LastWeightRecord = mapper.Map<HealhtyPaws.Models.Responses.DogWeight.DogWeightResponse>(latestWeight);
        return response;
    }

    public override async Task<IQueryable<Database.Entity.Device>> ApplyFilter(IQueryable<Database.Entity.Device> queryable, DeviceSearchObject searchObject)
    {
        queryable = await base.ApplyFilter(queryable, searchObject);
        var userId = authorizationService.GetUserId();
        return queryable.Where(d => dbContext.UserDevices.Any(ud => ud.DeviceId == d.Id && ud.UserId == userId));
    }

    public override async Task<IQueryable<Database.Entity.Device>> IncludeAdditionalEntities(IQueryable<Database.Entity.Device> queryable)
    {
        queryable = await base.IncludeAdditionalEntities(queryable);
        return queryable
            .Include(d => d.Dog).ThenInclude(d => d!.Breed)
            .Include(d => d.Dog).ThenInclude(d => d!.DogWeights!.OrderByDescending(w => w.CreatedAt).Take(1));
    }

    protected override async Task BeforeUpdate(int id, DeviceUpdateRequest request, Database.Entity.Device entity)
    {
        await base.BeforeUpdate(id, request, entity);
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.DeviceId == id && ud.UserId == userId))
            throw new NotFoundException("Nije pronađeno!");
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == request.DogId))
            throw new BadRequestException("Odabrani pas nije pronađen!");
    }

    protected override async Task PrepareForResponse(Database.Entity.Device entity, HealthyPawsDbContext dbContext)
    {
        await base.PrepareForResponse(entity, dbContext);
        entity.Dog = await dbContext.Dogs
            .Include(d => d.Breed)
            .Include(d => d.DogWeights!.OrderByDescending(w => w.CreatedAt).Take(1))
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == entity.DogId);
    }

    protected override async Task BeforeInsert(DeviceInsertRequest request)
    {
        await base.BeforeInsert(request);
        if (await dbContext.UserDevices.AnyAsync(d =>
                d.Device!.MacAddress == request.MacAddress && d.UserId == authorizationService.GetUserId()))
        {
            throw new BadRequestException("Već posjedujete uređaj s ovim MAC adresom!");
        }

        if (request is { Dog: not null, DogWeight: not null })
        {
            var mealTimes = request.MealTimes;
            if (mealTimes is not { Count: >= 1 and <= 5 })
                throw new BadRequestException("Unesite od 1 do 5 obroka dnevno!");
            if (mealTimes.Distinct().Count() != mealTimes.Count)
                throw new BadRequestException("Vremena obroka moraju biti različita!");
        }
    }
}