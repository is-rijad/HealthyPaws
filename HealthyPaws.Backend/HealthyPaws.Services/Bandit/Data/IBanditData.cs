using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Database.Entity.Bandit;

namespace HealthyPaws.Services.Bandit.Data;

public interface IBanditData
{
    Task<(int DogId, int MealIndex, BanditContext Context)> BuildContext(Guid decisionId);
    Task SaveContext(Guid decisionId, BanditContext ctx);
    Task<BanditContext> GetContext(Guid decisionId);
    Task<(int Version, string? Json)> GetState(int dogId);
    Task<bool> TryApplyState(int dogId, int expectedVersion, string stateJson);
    Task<bool> TryApplyFeedback(Guid decisionId, int dogId, int expectedVersion, string stateJson, double reward);
    Task<(int DogId, int Arm)> GetDecisionArm(Guid decisionId);
    Task<int> GetDogId(Guid decisionId);
}