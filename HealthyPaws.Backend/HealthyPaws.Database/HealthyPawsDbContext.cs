using HealthyPaws.Database.Entity;
using HealthyPaws.Database.Entity.CalorieAgent;
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
    public virtual DbSet<FeedingRecord> FeedingRecords { get; set; } = null!;

    public virtual DbSet<AgentDecisionLog> AgentDecisionLogs { get; set; } = null!;
    public virtual DbSet<Alert> Alerts { get; set; } = null!;
    public virtual DbSet<DogPlan> DogPlans { get; set; } = null!;
    public virtual DbSet<LearnedState> LearnedStates { get; set; } = null!;

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
        modelBuilder.ApplyConfiguration(new FeedingRecordEntityConfiguration());
        
        modelBuilder.ApplyConfiguration(new AgentDecisionLogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AlertEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DogPlanEntityConfiguration());
    }
}