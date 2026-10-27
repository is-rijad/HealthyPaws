using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;

    public ICollection<Dog>? Dogs { get; set; }
}

public class UserEntityConfiguration : IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(32);
        builder.Property(u => u.PasswordHash)
            .HasMaxLength(44)
            .IsUnicode(false)
            .IsFixedLength();
        builder.Property(u => u.PasswordSalt)
            .HasMaxLength(24)
            .IsUnicode(false)
            .IsFixedLength();
    }
}