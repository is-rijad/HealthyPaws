using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class Dog
{
    public int Id { get; set; }
    public int BreedId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public short Gender { get; set; }
    public float ActivityStatus { get; set; }
    public bool IsNeutered { get; set; }

    public Breed? Breed { get; set; }
    public ICollection<DogWeight>? DogWeights { get; set; }
}


public class DogEntityConfiguration : IEntityTypeConfiguration<Dog> {
    public void Configure(EntityTypeBuilder<Dog> builder) {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(16).UseCollation("Croatian_CI_AS").IsUnicode();
        builder.Property(u => u.DateOfBirth).IsRequired();
        builder.Property(u => u.Gender).IsRequired();
        builder.Property(u => u.ActivityStatus).IsRequired();
        builder.Property(u => u.IsNeutered).IsRequired();

        builder.HasOne(u => u.Breed).WithMany(b => b.Dogs).HasForeignKey(d => d.BreedId);

    }
}
