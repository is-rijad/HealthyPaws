namespace HealhtyPaws.Models.BaseModels;

public record PagedResult<T>(
    List<T> Items,
    int Count,
    bool HasMore
);