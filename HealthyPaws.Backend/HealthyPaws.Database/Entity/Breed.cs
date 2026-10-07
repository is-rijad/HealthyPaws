using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class Breed
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal MinWeight { get; set; }
    public decimal MaxWeight { get; set; }

    public ICollection<Dog>? Dogs { get; set; }
}

public class BreedEntityConfiguration : IEntityTypeConfiguration<Breed> {
    public void Configure(EntityTypeBuilder<Breed> builder) {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(64).UseCollation("Croatian_CI_AS").IsUnicode();
        builder.Property(b => b.MinWeight).IsRequired();
        builder.Property(b => b.MaxWeight).IsRequired();
        builder.HasData(BreedSeedData.All());
    }
}