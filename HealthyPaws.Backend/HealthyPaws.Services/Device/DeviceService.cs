using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Device;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Device;

public class DeviceService(HealthyPawsDbContext db, IMapper map, AuthorizationService auth,
    ICalorieAgentService calorieAgentService) : BaseCrudService<Database.Entity.Device, DeviceInsertRequest, DeviceUpdateRequest, DeviceResponse, DeviceSearchObject>(db, map, auth), IDeviceService
{
    public override async Task<DeviceResponse> InsertAsync(DeviceInsertRequest request) {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await BeforeInsert(request);

        var existingDevice = await dbContext.Devices.Include(d => d.Dog).FirstOrDefaultAsync(d => d.MacAddress == request.MacAddress);
        var dog = existingDevice?.Dog;
        if (request.Dog != null && request.DogWeight != null && request.MealTimes != null)
        {
            existingDevice = null;
            dog = mapper.Map<Database.Entity.Dog>(request.Dog);
            await dbContext.Dogs.AddAsync(dog);
            await dbContext.SaveChangesAsync();
            
            var dogWeightInsertRequest = mapper.Map<Database.Entity.DogWeight>(request.DogWeight);
            dogWeightInsertRequest.DogId = dog.Id;
            dogWeightInsertRequest.CreatedAt = DateTime.UtcNow;

            var decision = await calorieAgentService.TickAsync(dog, dogWeightInsertRequest);
            // Sumnjiva težina pri dodavanju uređaja: ništa se ne sprema, korisnik ispravlja težinu i pokušava ponovo.
            if (decision.Action == global::CalorieAgent.Action.RequestRemeasure)
                throw new BadRequestException(decision.Explanation);
            
            await dbContext.DogWeights.AddAsync(dogWeightInsertRequest);

            await dbContext.DogMeals.AddRangeAsync(request.MealTimes.OrderBy(it => it).Select((t, i) =>
                new Database.Entity.DogMeal
                {
                    DogId = dog.Id,
                    Time = t,
                    MealIndex = (short)i
                }));
            await dbContext.SaveChangesAsync();
        }

        if (dog is null)
            throw new BadRequestException("Uređaj nije pronađen. Unesite podatke o psu, težini i obrocima!");

        var device = existingDevice ?? (await dbContext.Devices.AddAsync(new Database.Entity.Device()
        {
            DogId = dog.Id,
            MacAddress = request.MacAddress,
            IsConnected = true
        })).Entity;
        await AfterInsert(device, request, dbContext);
        await dbContext.SaveChangesAsync();


        await dbContext.UserDevices.AddAsync(new Database.Entity.UserDevice()
        {
            DeviceId = device.Id,
            UserId = authorizationService.GetUserId()
        });

        await dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        await PrepareForResponse(device, dbContext);
        return MapToResponseModel<DeviceResponse>(device, mapper);
    }

    public async Task<string> GetDeviceMac(int dogId)
    {
        return (await dbContext.Devices.SingleAsync(it => it.DogId == dogId)).MacAddress;
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
            .Include(d => d.Dog).ThenInclude(d => d!.DogWeights!.Where(w => !w.IsRejected).OrderByDescending(w => w.CreatedAt).Take(1));
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
            .Include(d => d.DogWeights!.Where(w => !w.IsRejected).OrderByDescending(w => w.CreatedAt).Take(1))
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
            if (mealTimes is not { Count: >= 1 and <= 4 })
                throw new BadRequestException("Unesite od 1 do 4 obroka dnevno!");
            if (mealTimes.Distinct().Count() != mealTimes.Count)
                throw new BadRequestException("Vremena obroka moraju biti različita!");
            if (!MealTimesSpacedEnough(mealTimes))
                throw new BadRequestException("Vremena obroka moraju biti međusobno udaljena najmanje 3 sata!");
        }
    }

    private const int MinMealGapMinutes = 3 * 60;

    private static bool MealTimesSpacedEnough(List<TimeOnly> mealTimes)
    {
        var minutes = mealTimes.Select(t => t.Hour * 60 + t.Minute).OrderBy(m => m).ToList();
        if (minutes.Count < 2) return true;
        for (var i = 1; i < minutes.Count; i++)
            if (minutes[i] - minutes[i - 1] < MinMealGapMinutes)
                return false;
        return minutes[0] + 24 * 60 - minutes[^1] >= MinMealGapMinutes;
    }
}