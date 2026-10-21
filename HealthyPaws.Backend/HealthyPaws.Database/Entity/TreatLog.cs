using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class TreatLog
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public int? TreatId { get; set; }
    public string TreatName { get; set; } = null!;
    public int Quantity { get; set; }
    public double Calories { get; set; }
    public DateTime GivenAt { get; set; }

    public Dog? Dog { get; set; }
    public Treat? Treat { get; set; }
}

public class TreatLogEntityConfiguration : IEntityTypeConfiguration<TreatLog> {
    public void Configure(EntityTypeBuilder<TreatLog> builder) {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TreatName).IsRequired().HasMaxLength(64).UseCollation("Croatian_CI_AS").IsUnicode();
        builder.Property(t => t.Quantity).IsRequired();
        builder.Property(t => t.Calories).IsRequired();
        builder.Property(t => t.GivenAt).IsRequired();

        builder.HasOne(t => t.Dog).WithMany().HasForeignKey(t => t.DogId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Treat).WithMany().HasForeignKey(t => t.TreatId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(t => new { t.DogId, t.GivenAt });
    }
}
