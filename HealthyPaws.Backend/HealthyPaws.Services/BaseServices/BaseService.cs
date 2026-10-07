using HealhtyPaws.Models.BaseModels;
using HealthyPaws.Database;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.BaseServices;

public abstract class
    BaseService<TResponse, TEntity, TSearchObject>(HealthyPawsDbContext dbContext, IMapper mapper)
    : IBaseService<TResponse, TSearchObject>
    where TResponse : class
    where TEntity : class
    where TSearchObject : BaseSearchObject
{

    public virtual async Task<PagedResult<TResponse>> GetAsync(TSearchObject searchObject) {
        var queryable = dbContext.Set<TEntity>().AsNoTracking();
        queryable = await ApplyFilter(queryable, searchObject);
        queryable = ApplyOrdering(queryable, searchObject);
        var paginatedQueryable = await Paginate(queryable, searchObject);
        queryable = paginatedQueryable.Queryable;
        queryable = await IncludeAdditionalEntities(queryable);
        var list = await queryable.Select(e => MapToResponseModel<TResponse>(e, mapper)).ToListAsync();
        return new PagedResult<TResponse>(list, paginatedQueryable.PaginatedCount, paginatedQueryable.FullCount > (searchObject.Page - 1) * searchObject.PageSize + searchObject.PageSize);
    }

    public virtual async Task<TResponse> GetByIdAsync(int id) {
        var queryable = dbContext.Set<TEntity>().AsNoTracking();
        var entity = await queryable.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        if (entity == null)
            throw new NotFoundException("Nije pronađeno!");
        await PrepareForResponse(entity, dbContext);
        return MapToResponseModel<TResponse>(entity, mapper);
    }

    protected async Task<PaginatedQueryable<TEntity>> Paginate(IQueryable<TEntity> queryable,
        TSearchObject searchObject) {
        var fullCount = await queryable.CountAsync();
        queryable = queryable.Skip((searchObject.Page - 1) * searchObject.PageSize).Take(searchObject.PageSize);
        return new PaginatedQueryable<TEntity>(queryable, fullCount, await queryable.CountAsync());
    }

    protected virtual IQueryable<TEntity> ApplyOrdering(IQueryable<TEntity> queryable, TSearchObject searchObject) {
        return queryable.OrderByDescending(it => EF.Property<int>(it, "Id")).AsQueryable();
    }

    public virtual Task<IQueryable<TEntity>> ApplyFilter(IQueryable<TEntity> queryable,
        TSearchObject searchObject) {
        return Task.FromResult(queryable);
    }

    public virtual Task<IQueryable<TEntity>> IncludeAdditionalEntities(IQueryable<TEntity> queryable) {
        return Task.FromResult(queryable);
    }

    protected virtual Task PrepareForResponse(TEntity entity, HealthyPawsDbContext dbContext) {
        return Task.CompletedTask;
    }

    protected static T MapToResponseModel<T>(TEntity entity, IMapper mapper) {
        return mapper.Map<T>(entity);
    }
}