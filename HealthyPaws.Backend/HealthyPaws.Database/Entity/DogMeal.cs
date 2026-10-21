using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class DogMeal
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public TimeOnly Time { get; set; }

    public Dog? Dog { get; set; }
}

public class DogMealEntityConfiguration : IEntityTypeConfiguration<DogMeal> {
    public void Configure(EntityTypeBuilder<DogMeal> builder) {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Time).IsRequired();

        builder.HasOne(m => m.Dog).WithMany().HasForeignKey(m => m.DogId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.DogId, m.Time }).IsUnique();
    }
}
