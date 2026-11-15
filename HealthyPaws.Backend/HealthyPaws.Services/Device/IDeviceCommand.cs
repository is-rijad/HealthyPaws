using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;

namespace HealthyPaws.Services.Device;

public interface IDeviceCommand
{
    Task Dispense(Guid decisionId, int dogId);
    Task<OutcomeResult> ReadOutcome(Guid decisionId);
}