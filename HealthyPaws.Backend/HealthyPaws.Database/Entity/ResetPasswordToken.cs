using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class ResetPasswordToken
{
    public int UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public string TokenSalt { get; set; } = null!;
    public DateTime Expiration { get; set; }

    public User? User { get; set; }
}

public class ResetPasswordTokenEntityConfiguration : IEntityTypeConfiguration<ResetPasswordToken> {
    public void Configure(EntityTypeBuilder<ResetPasswordToken> builder) {
        builder.HasKey(rpt => rpt.UserId);
        builder.Property(rpt => rpt.TokenHash).IsRequired().HasMaxLength(44).IsUnicode(false).IsFixedLength();
        builder.Property(rpt => rpt.TokenSalt).IsRequired().HasMaxLength(24).IsUnicode(false).IsFixedLength();
        builder.Property(rpt => rpt.Expiration).IsRequired();
        builder.HasOne(rpt => rpt.User)
            .WithMany()
            .HasForeignKey(rpt => rpt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}