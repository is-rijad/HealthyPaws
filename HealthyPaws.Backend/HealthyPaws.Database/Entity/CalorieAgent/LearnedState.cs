namespace HealthyPaws.Database.Entity.CalorieAgent;

public class LearnedState
{
    public double? MerEstimateKcal { get; set; }
    public double? VarianceKcal2 { get; set; }
    public int NUpdates { get; set; }
    public int ConsecutiveBadWeeks { get; set; }
    public int WeeksAtBound { get; set; }
    public double? PendingWeightKg { get; set; }
    public DateTimeOffset? LastUpdateAt { get; set; }
}