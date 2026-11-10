using CalorieAgent;

namespace HealthyPaws.Services.Mapster;

public static class LearnedStateProtoExtension
{
    public static Database.Entity.CalorieAgent.LearnedState ToEntity(this LearnedState state)
    {
        var entity = new Database.Entity.CalorieAgent.LearnedState();
        entity.CopyFrom(state);
        return entity;
    }

    public static void CopyFrom(this Database.Entity.CalorieAgent.LearnedState entity, LearnedState state)
    {
        entity.ConsecutiveBadWeeks = state.ConsecutiveBadWeeks;
        entity.LastUpdateAt = state.LastUpdateAt != null ? DateTimeOffset.FromUnixTimeSeconds(state.LastUpdateAt.Seconds) : null;
        entity.MerEstimateKcal = state.HasMerEstimateKcal ? state.MerEstimateKcal : null;
        entity.NUpdates = state.NUpdates;
        entity.PendingWeightKg = state.HasPendingWeightKg ? state.PendingWeightKg : null;
        entity.VarianceKcal2 = state.HasVarianceKcal2 ? state.VarianceKcal2 : null;
        entity.WeeksAtBound = state.WeeksAtBound;
    }
}
