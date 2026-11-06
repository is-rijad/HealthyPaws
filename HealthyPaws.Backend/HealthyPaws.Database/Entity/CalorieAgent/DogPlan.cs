using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity.CalorieAgent;

public class DogPlan
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public double DailyKcal { get; set; }
    public double DailyGrams { get; set; }

    public int PlanVersion { get; set; }

    public LearnedState? Learned { get; set; }

    public DateTimeOffset? LastAppliedMeasuredAt { get; set; }
    public DateTime NextMeasureAt { get; set; }
}

public class DogPlanEntityConfiguration : IEntityTypeConfiguration<DogPlan> {
    public void Configure(EntityTypeBuilder<DogPlan> builder) {
        builder.HasKey(b => b.Id);
        builder.Property(x => x.PlanVersion).IsConcurrencyToken();
        builder.OwnsOne(x => x.Learned);
    }
}
