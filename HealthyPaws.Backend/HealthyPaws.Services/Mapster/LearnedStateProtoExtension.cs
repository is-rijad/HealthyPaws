
using CalorieAgent;

namespace HealthyPaws.Services.Mapster;

public static class LearnedStateProtoExtension
{
    public static Database.Entity.CalorieAgent.LearnedState ToEntity(this LearnedState state)
    {
        return new()
        {
            ConsecutiveBadWeeks = state.ConsecutiveBadWeeks,
            LastUpdateAt = state.LastUpdateAt != null ? DateTimeOffset.FromUnixTimeMilliseconds(state.LastUpdateAt!.Seconds) : null ,
            MerEstimateKcal = state.MerEstimateKcal,
            NUpdates = state.NUpdates,
            PendingWeightKg = state.PendingWeightKg,
            VarianceKcal2 = state.VarianceKcal2,
            WeeksAtBound = state.WeeksAtBound,
        };
    } 
}