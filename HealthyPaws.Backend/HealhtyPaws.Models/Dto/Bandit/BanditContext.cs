using System.Text.Json.Serialization;

namespace HealhtyPaws.Models.Dto.Bandit;

public record BanditContext(
    [property: JsonPropertyName("hoursSinceLastMeal")] double? HoursSinceLastMeal,
    [property: JsonPropertyName("avgEatenFraction")] double? AvgEatenFraction,
    [property: JsonPropertyName("treatBudgetShare")] double? TreatBudgetShare,
    [property: JsonPropertyName("baselineHour")] double BaselineHour,
    [property: JsonPropertyName("mealIndex")] int MealIndex);
