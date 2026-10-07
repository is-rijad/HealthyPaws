using HealhtyPaws.Models.Responses.Breed;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Services.BaseServices;
using MapsterMapper;

namespace HealthyPaws.Services.Breed;

public class BreedService(HealthyPawsDbContext dbContext, IMapper mapper) : BaseService<BreedResponse, Database.Entity.Breed, BreedSearchObject>(dbContext, mapper), IBreedService
{
    public override async Task<IQueryable<Database.Entity.Breed>> ApplyFilter(IQueryable<Database.Entity.Breed> queryable, BreedSearchObject searchObject) {
        queryable = await base.ApplyFilter(queryable, searchObject);
        if (!string.IsNullOrEmpty(searchObject.Contains)) {
            queryable = queryable.Where(b => b.Name.ToLower().Contains(searchObject.Contains.ToLower()));
        }
        return queryable;
    }
    protected override IQueryable<Database.Entity.Breed> ApplyOrdering(IQueryable<Database.Entity.Breed> queryable, BreedSearchObject searchObject) {
        queryable = base.ApplyOrdering(queryable, searchObject);
        queryable = queryable.OrderBy(b => b.Name).ThenBy(b => b.Id);
        return queryable;
    }
}