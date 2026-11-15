using System.Text.Json.Serialization;

namespace HealhtyPaws.Models.Dto.Bandit.Outcome;

public record OutcomePayload(
    [property: JsonPropertyName("dispensedG")] double DispensedG,
    [property: JsonPropertyName("eatenG")] double EatenG,
    [property: JsonPropertyName("latencyS")] double? LatencyS);