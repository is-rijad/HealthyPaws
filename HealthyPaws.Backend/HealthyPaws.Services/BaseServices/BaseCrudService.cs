using HealhtyPaws.Models.BaseModels;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;

namespace HealthyPaws.Services.BaseServices;


public abstract class
    BaseCrudService<TEntity, TInsertRequest, TUpdateRequest, TResponse, TSearchObject>(
        HealthyPawsDbContext dbContext,
        IMapper mapper,
        AuthorizationService authorizationService)
    :
        BaseService<TResponse, TEntity, TSearchObject>(dbContext, mapper),
        IBaseCrudService<TInsertRequest, TUpdateRequest, TResponse, TSearchObject>
    where TEntity : class
    where TInsertRequest : class
    where TUpdateRequest : class
    where TResponse : class
    where TSearchObject : BaseSearchObject
{

    public virtual async Task<TResponse> InsertAsync(TInsertRequest request) {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        await BeforeInsert(request);
        var entityEntry = await dbContext.Set<TEntity>().AddAsync(MapToInsertEntity(request));

        await AfterInsert(entityEntry.Entity, request, dbContext);
        await dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        await PrepareForResponse(entityEntry.Entity, dbContext);
        return MapToResponseModel<TResponse>(entityEntry.Entity, mapper);
    }

    public virtual async Task<TResponse> UpdateAsync(int id, TUpdateRequest request) {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var entity = await dbContext.Set<TEntity>().FindAsync(id);
        if (entity == null)
            throw new NotFoundException("Nije pronađeno!");
        await BeforeUpdate(id, request, entity);

        MapToUpdateEntity(request, entity);

        await AfterUpdate(entity);
        await dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        await PrepareForResponse(entity, dbContext);
        return MapToResponseModel<TResponse>(entity, mapper);
    }

    public virtual async Task DeleteAsync(int id) {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbSet = dbContext.Set<TEntity>();
        var entity = await dbContext.Set<TEntity>().FindAsync(id);
        if (entity == null)
            throw new NotFoundException("Nije pronađeno!");
        await BeforeDelete(id, entity);
        dbSet.Remove(entity);
        AfterDelete();

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    protected virtual Task BeforeInsert(TInsertRequest request) {
        return Task.CompletedTask;
    }

    protected virtual Task AfterInsert(TEntity entity, TInsertRequest request, HealthyPawsDbContext dbContext) {
        return Task.CompletedTask;
    }

    protected virtual TEntity MapToInsertEntity(TInsertRequest request) {
        return mapper.Map<TEntity>(request);
    }

    protected virtual Task BeforeUpdate(int id, TUpdateRequest request, TEntity entity) {
        return Task.CompletedTask;
    }

    protected virtual Task AfterUpdate(TEntity entity) {
        return Task.CompletedTask;
    }

    protected virtual TEntity MapToUpdateEntity(TUpdateRequest request, TEntity entity) {
        return mapper.Map(request, entity);
    }

    protected virtual Task BeforeDelete(int id, TEntity entity) {
        return Task.CompletedTask;
    }

    protected virtual void AfterDelete() {
    }
}