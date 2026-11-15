using System.Text.Json.Serialization;

namespace HealhtyPaws.Models.Dto.Bandit;

public record SelectResponse(
    [property: JsonPropertyName("decisionId")] string DecisionId,
    [property: JsonPropertyName("dogId")] string DogId,
    [property: JsonPropertyName("arm")] int? Arm,
    [property: JsonPropertyName("stateVersion")] int StateVersion);  