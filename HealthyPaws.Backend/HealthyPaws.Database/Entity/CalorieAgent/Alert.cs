using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity.CalorieAgent;

public enum AlertResolvedBy
{
    Agent,
    User
}
public class Alert
{
    public long Id { get; set; }
    public int DogId { get; set; }
    public string Reason { get; set; } = null!;
    public string Explanation { get; set; } = null!;
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public int Occurrences { get; set; } = 1;
    public DateTimeOffset? ResolvedAt { get; set; }
    public AlertResolvedBy? ResolvedBy { get; set; }
}

public class AlertEntityConfiguration : IEntityTypeConfiguration<Alert> {
    public void Configure(EntityTypeBuilder<Alert> builder) {
        builder.HasKey(b => b.Id);
        builder.Property(x => x.Reason).HasMaxLength(64);
        builder.Property(x => x.Explanation).HasMaxLength(512);
        builder.HasIndex(x => new { x.DogId, x.Reason }).IsUnique().HasFilter("[ResolvedAt] IS NULL");
    }
}