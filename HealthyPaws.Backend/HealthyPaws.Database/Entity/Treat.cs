using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class Treat
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public double CaloriesPerPiece { get; set; }

    public User? User { get; set; }
}

public class TreatEntityConfiguration : IEntityTypeConfiguration<Treat> {
    public void Configure(EntityTypeBuilder<Treat> builder) {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(64).UseCollation("Croatian_CI_AS").IsUnicode();
        builder.Property(t => t.CaloriesPerPiece).IsRequired();

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId);
        builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
    }
}
