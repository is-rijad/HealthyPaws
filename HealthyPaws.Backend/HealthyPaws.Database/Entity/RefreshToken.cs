using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class RefreshToken
{
    public int UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public string TokenSalt { get; set; } = null!;

    public User? User { get; set; }
}

public class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshToken> {
    public void Configure(EntityTypeBuilder<RefreshToken> builder) {
        builder.HasKey(rt => rt.UserId);
        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(44).IsUnicode(false).IsFixedLength();
        builder.Property(rt => rt.TokenSalt).IsRequired().HasMaxLength(24).IsUnicode(false).IsFixedLength();
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}