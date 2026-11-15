using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;

namespace HealthyPaws.Services.Bandit.Publisher;

public interface IBanditPublisher
{
    Task PublishSelect(Guid decisionId);
    Task PublishFeedback(Guid decisionId, OutcomeDto o);
    Task PublishDispense(Guid decisionId, int dogId);
}