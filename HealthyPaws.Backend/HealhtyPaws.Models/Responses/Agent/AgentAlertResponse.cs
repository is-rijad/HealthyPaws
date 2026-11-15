namespace HealhtyPaws.Models.Responses.Agent;

public record AgentAlertResponse(
    long Id,
    int DogId,
    string Reason,
    string Explanation,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int Occurrences,
    DateTimeOffset? ResolvedAt,
    string? ResolvedBy,
    string Category,
    string RecommendedAction);
