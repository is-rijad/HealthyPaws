namespace HealhtyPaws.Models.BaseModels;

public record PaginatedQueryable<T>(
    IQueryable<T> Queryable,
    int FullCount,
    int PaginatedCount
) where T : class;