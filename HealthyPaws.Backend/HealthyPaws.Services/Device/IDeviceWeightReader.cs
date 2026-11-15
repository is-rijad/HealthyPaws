using HealhtyPaws.Models.Dto.Bandit.Outcome;

namespace HealthyPaws.Services.Device;

public interface IDeviceWeightReader
{
    Task<IReadOnlyList<Sample>> Read(int dogId, DateTimeOffset from, DateTimeOffset to);
}