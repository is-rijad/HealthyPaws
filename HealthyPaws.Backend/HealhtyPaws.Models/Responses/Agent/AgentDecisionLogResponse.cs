namespace HealhtyPaws.Models.Responses.Agent;

public record AgentDecisionLogResponse(
    long Id,
    int DogId,
    DateTimeOffset MeasuredAt,
    string Action,
    string Outcome,
    string ReasonCodes,
    string Explanation,
    double? NewDailyKcal,
    double? NewDailyGrams,
    bool IsRejected,
    bool RequiresHumanReview,
    DateTimeOffset ReceivedAt);
