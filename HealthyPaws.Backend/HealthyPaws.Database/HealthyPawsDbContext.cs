using HealthyPaws.Database.Entity;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Database;

public class HealthyPawsDbContext(DbContextOptions<HealthyPawsDbContext> options) : DbContext(options)
{
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public virtual DbSet<ResetPasswordToken> ResetPasswordTokens { get; set; } = null!;
    public virtual DbSet<Dog> Dogs { get; set; } = null!;
    public virtual DbSet<UserDog> UserDogs { get; set; } = null!;
    public virtual DbSet<Breed> Breeds { get; set; } = null!;
    public virtual DbSet<Device> Devices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ResetPasswordTokenEntityConfiguration());

        modelBuilder.ApplyConfiguration(new DogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserDogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BreedEntityConfiguration());

        modelBuilder.ApplyConfiguration(new DeviceEntityConfiguration());
    }
}