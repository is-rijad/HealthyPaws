using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Dog;
using HealhtyPaws.Models.Responses.Dog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using MapsterMapper;

namespace HealthyPaws.Services.Dog;

public class DogService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService) : BaseCrudService<Database.Entity.Dog, DogInsertRequest, DogInsertRequest, DogResponse, DogSearchObject>(dbContext, mapper, authorizationService), IDogService
{
    public override async Task<IQueryable<Database.Entity.Dog>> ApplyFilter(IQueryable<Database.Entity.Dog> queryable, DogSearchObject searchObject)
    {
        queryable = await base.ApplyFilter(queryable, searchObject);
        if (!string.IsNullOrEmpty(searchObject.Contains))
        {
            queryable = queryable.Where(d => d.Name.Contains(searchObject.Contains, StringComparison.InvariantCultureIgnoreCase));
        }
        return queryable;
    }

    protected override IQueryable<Database.Entity.Dog> ApplyOrdering(IQueryable<Database.Entity.Dog> queryable, DogSearchObject searchObject)
    {
        queryable = base.ApplyOrdering(queryable, searchObject);
        queryable = queryable.OrderByDescending(d => d.Id);
        return queryable;
    }
}