using System.Text.Json;
using System.Text.Json.Serialization;

namespace HealhtyPaws.Models.Dto.Bandit;

public record SelectRequest([property: JsonPropertyName("decisionId")] string DecisionId,
    [property: JsonPropertyName("dogId")] string DogId,
    [property: JsonPropertyName("context")] BanditContext Context,
    [property: JsonPropertyName("arms")] int[] Arms,
    [property: JsonPropertyName("state")] JsonElement? State);