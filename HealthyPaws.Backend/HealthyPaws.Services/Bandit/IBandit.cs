using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Database.Entity.Bandit;

namespace HealthyPaws.Services.Bandit;

public interface IBandit
{
    Task<IReadOnlyList<(int DogId, int MealIndex, DateTimeOffset BaselineUtc)>> MealsForDay(DateOnly day);
    Task<bool> TryCreate(Guid id, int dogId, int mealIndex, DateOnly day, DateTimeOffset baselineUtc);
    Task<DecisionDto?> Get(Guid id);
    Task<IReadOnlyList<DecisionDto>> ListChosen();
    Task<bool> Transition(Guid id, DecisionStatus from, DecisionStatus to);
    Task<bool> TryChoose(Guid id, int arm, DecisionStatus[] allowedFrom, bool byFallback = false);
    Task SaveOutcome(Guid id, OutcomeDto o);
}
