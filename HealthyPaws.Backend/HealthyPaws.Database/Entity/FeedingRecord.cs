using HealthyPaws.Database.Entity.CalorieAgent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class FeedingRecord
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public int PlanId { get; set; }
    public double EatenG { get; set; }
    public DateTime EatenAt { get; set; }
    public double OfferedG { get; set; }
    public DogPlan? Plan { get; set; }
    public Dog? Dog { get; set; }
}


public class FeedingRecordEntityConfiguration : IEntityTypeConfiguration<FeedingRecord> {
    public void Configure(EntityTypeBuilder<FeedingRecord> builder) {
        builder.HasKey(b => b.Id);
        builder.HasOne(b => b.Dog).WithMany().HasForeignKey(b => b.DogId);
        builder.HasOne(b => b.Plan).WithMany().HasForeignKey(b => b.PlanId);
        builder.HasIndex(x => new { x.DogId, x.EatenAt }).IsUnique();
    }
}