using HealhtyPaws.Models.Requests.TreatLog;
using HealhtyPaws.Models.Responses.TreatLog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.TreatLog;

public class TreatLogService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService) : BaseCrudService<Database.Entity.TreatLog, TreatLogInsertRequest, TreatLogInsertRequest, TreatLogResponse, TreatLogSearchObject>(dbContext, mapper, authorizationService), ITreatLogService
{
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    public override async Task<IQueryable<Database.Entity.TreatLog>> ApplyFilter(IQueryable<Database.Entity.TreatLog> queryable, TreatLogSearchObject searchObject)
    {
        queryable = await base.ApplyFilter(queryable, searchObject);
        var userId = authorizationService.GetUserId();
        queryable = queryable.Where(l => dbContext.UserDevices.Any(ud => ud.UserId == userId && ud.Device!.DogId == l.DogId));
        if (searchObject.DogId != null)
            queryable = queryable.Where(l => l.DogId == searchObject.DogId);
        if (searchObject.From != null)
        {
            var from = searchObject.From.Value.ToUniversalTime();
            queryable = queryable.Where(l => l.GivenAt >= from);
        }
        if (searchObject.To != null)
        {
            var to = searchObject.To.Value.ToUniversalTime();
            queryable = queryable.Where(l => l.GivenAt < to);
        }
        return queryable;
    }

    protected override IQueryable<Database.Entity.TreatLog> ApplyOrdering(IQueryable<Database.Entity.TreatLog> queryable, TreatLogSearchObject searchObject)
    {
        return queryable.OrderByDescending(l => l.GivenAt).ThenByDescending(l => l.Id);
    }

    public override async Task<TreatLogResponse> InsertAsync(TreatLogInsertRequest request)
    {
        var userId = authorizationService.GetUserId();

        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == request.DogId))
            throw new NotFoundException("Pas nije pronađen!");

        var treat = await dbContext.Treats.FirstOrDefaultAsync(t => t.Id == request.TreatId && t.UserId == userId)
                    ?? throw new NotFoundException("Poslastica nije pronađena!");

        var givenAt = request.GivenAt?.ToUniversalTime() ?? DateTime.UtcNow;
        if (givenAt > DateTime.UtcNow.Add(FutureTolerance))
            throw new BadRequestException("Vrijeme davanja ne može biti u budućnosti!");

        var entity = new Database.Entity.TreatLog
        {
            DogId = request.DogId,
            TreatId = treat.Id,
            TreatName = treat.Name,
            Quantity = request.Quantity,
            Calories = Math.Round(request.Quantity * treat.CaloriesPerPiece, 2),
            GivenAt = givenAt
        };
        await dbContext.TreatLogs.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        return MapToResponseModel<TreatLogResponse>(entity, mapper);
    }

    public async Task<double> GetTreatCaloriesAsync(int dogId, DateTime? from, DateTime? to)
    {
        var periodStart = from?.ToUniversalTime() ?? DateTime.UtcNow.Date;
        var periodEnd = to?.ToUniversalTime() ?? periodStart.AddDays(1);
        
        var treatCalories = await dbContext.TreatLogs
            .Where(l => l.DogId == dogId && l.GivenAt >= periodStart && l.GivenAt < periodEnd)
            .SumAsync(l => l.Calories);
        
        return Math.Round(treatCalories, 1);
    }

    protected override async Task BeforeDelete(int id, Database.Entity.TreatLog entity)
    {
        await base.BeforeDelete(id, entity);
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == entity.DogId))
            throw new NotFoundException("Nije pronađeno!");
    }
}
