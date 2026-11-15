using System.Text.Json;
using System.Text.Json.Serialization;

namespace HealhtyPaws.Models.Dto.Bandit;

public record FeedbackResponse(
    [property: JsonPropertyName("decisionId")] string DecisionId,
    [property: JsonPropertyName("dogId")] string DogId,
    [property: JsonPropertyName("reward")] double Reward,
    [property: JsonPropertyName("state")] JsonElement State);       