using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class UserDog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DogId { get; set; }

    public User? User { get; set; }
    public Dog? Dog { get; set; }
}

public class UserDogEntityConfiguration : IEntityTypeConfiguration<UserDog> {
    public void Configure(EntityTypeBuilder<UserDog> builder) {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserId).IsRequired();
        builder.Property(u => u.DogId).IsRequired();

        builder.HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId);
        builder.HasOne(u => u.Dog).WithMany().HasForeignKey(u => u.DogId);

    }
}