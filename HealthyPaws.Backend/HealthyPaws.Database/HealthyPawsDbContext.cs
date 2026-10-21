using HealthyPaws.Database.Entity;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Database;

public class HealthyPawsDbContext(DbContextOptions<HealthyPawsDbContext> options) : DbContext(options)
{
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public virtual DbSet<ResetPasswordToken> ResetPasswordTokens { get; set; } = null!;
    public virtual DbSet<Dog> Dogs { get; set; } = null!;
    public virtual DbSet<UserDevice> UserDevices { get; set; } = null!;
    public virtual DbSet<Breed> Breeds { get; set; } = null!;
    public virtual DbSet<Device> Devices { get; set; } = null!;
    public virtual DbSet<DogWeight> DogWeights { get; set; } = null!;
    public virtual DbSet<DogMeal> DogMeals { get; set; } = null!;
    public virtual DbSet<Treat> Treats { get; set; } = null!;
    public virtual DbSet<TreatLog> TreatLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ResetPasswordTokenEntityConfiguration());

        modelBuilder.ApplyConfiguration(new DogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserDeviceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BreedEntityConfiguration());

        modelBuilder.ApplyConfiguration(new DeviceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DogWeightEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DogMealEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TreatEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TreatLogEntityConfiguration());
    }
}