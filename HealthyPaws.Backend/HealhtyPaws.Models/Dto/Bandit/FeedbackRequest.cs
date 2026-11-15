using System.Text.Json;
using System.Text.Json.Serialization;
using HealhtyPaws.Models.Dto.Bandit.Outcome;

namespace HealhtyPaws.Models.Dto.Bandit;

public record FeedbackRequest(
    [property: JsonPropertyName("decisionId")] string DecisionId,
    [property: JsonPropertyName("dogId")] string DogId,
    [property: JsonPropertyName("arm")] int Arm,
    [property: JsonPropertyName("context")] BanditContext Context,
    [property: JsonPropertyName("outcome")] OutcomePayload Outcome,
    [property: JsonPropertyName("state")] JsonElement? State);