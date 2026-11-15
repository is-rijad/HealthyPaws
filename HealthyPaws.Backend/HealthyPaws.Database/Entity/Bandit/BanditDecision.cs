using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity.Bandit;

public enum DecisionStatus { Planned, Requested, Chosen, Dispensed, FeedbackSent, Completed, NoData, Skipped }

public class BanditDecision
{
    public Guid Id { get; set; }
    public int DogId { get; set; }
    public int MealIndex { get; set; }
    public DateOnly Day { get; set; }
    public DateTimeOffset BaselineUtc { get; set; }
    public DecisionStatus Status { get; set; }

    public int? Arm { get; set; }
    public bool ChosenByFallback { get; set; }

    public DateTimeOffset? ContextSavedAt { get; set; }
    public double? HoursSinceLastMeal { get; set; }
    public double? AvgEatenFraction { get; set; }
    public double? TreatBudgetShare { get; set; }
    public double? BaselineHour { get; set; }

    public double? DispensedG { get; set; }
    public double? EatenG { get; set; }
    public double? LatencyS { get; set; }
    public double? Reward { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }
    public DateTimeOffset? ChosenAt { get; set; }
    public DateTimeOffset? DispensedAt { get; set; }
    public DateTimeOffset? FeedbackSentAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}


public class BanditDecisionConfig : IEntityTypeConfiguration<BanditDecision>
{
    public void Configure(EntityTypeBuilder<BanditDecision> b)
    {
        b.ToTable("BanditDecisions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.HasIndex(x => new { x.DogId, x.MealIndex, x.Day }).IsUnique();
        b.HasIndex(x => new { x.DogId, x.BaselineUtc });

        b.HasOne<Dog>().WithMany().HasForeignKey(x => x.DogId).OnDelete(DeleteBehavior.Cascade);
    }
}