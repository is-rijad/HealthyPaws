using System.Data.Common;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Database;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Device;

public class DeviceWeightReader(HealthyPawsDbContext dbContext) : IDeviceWeightReader
{
    public async Task<IReadOnlyList<Sample>> Read(int dogId, DateTimeOffset from, DateTimeOffset to)
    {
        return await dbContext.FeedingRecords.Where(it => it.DogId == dogId
                                                           && it.EatenAt >= from && it.EatenAt <= to)
            .Select(it => new Sample(it.EatenAt.ToUniversalTime(), it.EatenG)).ToListAsync();
    }
}