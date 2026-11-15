using HealthyPaws.Database.Entity.Bandit;

namespace HealhtyPaws.Models.Dto.Bandit;

public record DecisionDto(Guid Id, int DogId, int MealIndex, DateTimeOffset BaselineUtc, int? Arm, DecisionStatus Status);
