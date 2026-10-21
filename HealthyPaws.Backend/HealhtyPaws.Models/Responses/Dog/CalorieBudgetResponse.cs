namespace HealhtyPaws.Models.Responses.Dog;

public record CalorieBudgetResponse(int DogId, double DailyBudget, double TreatCalories, double Remaining);
