using HealhtyPaws.Models.Requests.Treat;
using HealhtyPaws.Models.Responses.Treat;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Treat;

public class TreatService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService) : BaseCrudService<Database.Entity.Treat, TreatInsertRequest, TreatInsertRequest, TreatResponse, TreatSearchObject>(dbContext, mapper, authorizationService), ITreatService
{
    public override async Task<IQueryable<Database.Entity.Treat>> ApplyFilter(IQueryable<Database.Entity.Treat> queryable, TreatSearchObject searchObject)
    {
        queryable = await base.ApplyFilter(queryable, searchObject);
        var userId = authorizationService.GetUserId();
        queryable = queryable.Where(t => t.UserId == userId);
        if (!string.IsNullOrEmpty(searchObject.Contains))
        {
            queryable = queryable.Where(t => t.Name.ToLower().Contains(searchObject.Contains.ToLower()));
        }
        return queryable;
    }

    protected override IQueryable<Database.Entity.Treat> ApplyOrdering(IQueryable<Database.Entity.Treat> queryable, TreatSearchObject searchObject)
    {
        return queryable.OrderBy(t => t.Name).ThenBy(t => t.Id);
    }

    protected override Database.Entity.Treat MapToInsertEntity(TreatInsertRequest request)
    {
        var entity = base.MapToInsertEntity(request);
        entity.UserId = authorizationService.GetUserId();
        return entity;
    }

    protected override async Task BeforeInsert(TreatInsertRequest request)
    {
        await base.BeforeInsert(request);
        await EnsureNameIsFree(request.Name, null);
    }

    protected override async Task BeforeUpdate(int id, TreatInsertRequest request, Database.Entity.Treat entity)
    {
        await base.BeforeUpdate(id, request, entity);
        EnsureOwned(entity);
        await EnsureNameIsFree(request.Name, id);
    }

    protected override Task BeforeDelete(int id, Database.Entity.Treat entity)
    {
        EnsureOwned(entity);
        return base.BeforeDelete(id, entity);
    }

    private void EnsureOwned(Database.Entity.Treat entity)
    {
        if (entity.UserId != authorizationService.GetUserId())
            throw new NotFoundException("Nije pronađeno!");
    }

    private async Task EnsureNameIsFree(string name, int? exceptId)
    {
        var userId = authorizationService.GetUserId();
        if (await dbContext.Treats.AnyAsync(t => t.UserId == userId && t.Id != exceptId && t.Name.ToLower() == name.ToLower()))
            throw new BadRequestException("Poslastica s ovim imenom već postoji!");
    }
}
