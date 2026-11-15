using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity.Bandit;

public class BanditState
{
    public int DogId { get; set; } 
    public int Version { get; set; }
    public string StateJson { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }

    public Dog? Dog { get; set; }
}

public class BanditStateConfig : IEntityTypeConfiguration<BanditState>
{
    public void Configure(EntityTypeBuilder<BanditState> b)
    {
        b.HasKey(x => x.DogId);
        b.Property(x => x.DogId).ValueGeneratedNever();
        b.Property(x => x.StateJson).IsRequired().HasMaxLength(int.MaxValue);
        b.HasOne<Dog>(x => x.Dog).WithOne().HasForeignKey<BanditState>(x => x.DogId).OnDelete(DeleteBehavior.Cascade);
    }
}