namespace HealhtyPaws.Models.Responses.Agent;

public record AgentDecisionResponse(
    string Action,
    bool PlanChanged,
    IReadOnlyList<string> ReasonCodes,
    string Explanation,
    double? NewDailyKcal,
    double? NewDailyGrams,
    bool RequiresHumanReview,
    DateTimeOffset NextEvaluationAt);
