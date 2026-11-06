namespace HealhtyPaws.Models.Responses.Calorie;

public record CalorieBudgetResponse(int DogId, double DailyBudget, double TreatCalories, double FoodCalories)
{
    public double Remaining => Math.Round(DailyBudget - (TreatCalories + FoodCalories), 1);
};