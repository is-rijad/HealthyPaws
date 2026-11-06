using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity.CalorieAgent;

public enum AgentAction
{
    ActionUnspecified,
    NoChange,
    Increase,
    Decrease,
    SetInitial,
    Escalate,
    RequestRemeasure,
}

public enum AgentOutcome
{
    NoPlanChange,
    PlanApplied,
    PlanVersionMismatch,
    Duplicate,
    Stale
}

public class AgentDecisionLog
{
    public long Id { get; set; }
    public int DogId { get; set; }
    public DateTimeOffset MeasuredAt { get; set; }
    public string CorrelationId { get; set; } = null!;
    public AgentAction Action { get; set; }
    public AgentOutcome Outcome { get; set; }
    public string ReasonCodes { get; set; } = null!;
    public string Explanation { get; set; } = null!;
    public double? NewDailyKcal { get; set; }
    public double? NewDailyGrams { get; set; }
    public int PlanVersionInResponse { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public bool IsRejected { get; set; }
}

public class AgentDecisionLogEntityConfiguration : IEntityTypeConfiguration<AgentDecisionLog> {
    public void Configure(EntityTypeBuilder<AgentDecisionLog> builder) {
        builder.HasKey(b => b.Id);
        builder.HasIndex(x => new { x.DogId, x.MeasuredAt }).IsUnique();
        
        builder.Property(x => x.CorrelationId).HasMaxLength(36);
        builder.Property(x => x.ReasonCodes).HasMaxLength(128);
        builder.Property(x => x.Explanation).HasMaxLength(512);
    }
}