using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public enum CalorieSource { Label = 0, Estimated = 1 }

public class DogWeight {
    public int Id { get; set; }
    public int DogId { get; set; }
    public int Bcs { get; set; }
    public double Weight { get; set; }
    public double Calorie { get; set; }
    /// <summary>Da li je kcal hrane deklarirano na etiketi ili procijenjeno iz sastava.</summary>
    public CalorieSource CalorieSource { get; set; } = CalorieSource.Label;
    public double? EatenGPerDay { get; set; }
    public double? OfferedGPerDay { get; set; }
    /// <summary>Agent je zatražio ponovno vaganje; mjerenje se ne koristi kao trenutna težina.</summary>
    public bool IsRejected { get; set; }
    public DateTime CreatedAt { get; set; }

    public Dog? Dog { get; set; }
}

public class DogWeightEntityConfiguration : IEntityTypeConfiguration<DogWeight> {
    public void Configure(EntityTypeBuilder<DogWeight> builder) {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Bcs).IsRequired();
        builder.Property(d => d.Weight).IsRequired();
        builder.Property(d => d.Calorie).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();

        builder.HasOne(d => d.Dog).WithMany(d => d.DogWeights).HasForeignKey(d => d.DogId);
    }
}
