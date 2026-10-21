using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class Device
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public string MacAddress { get; set; } = null!;
    public bool IsConnected { get; set; }
    public Dog? Dog { get; set; }
}

public class DeviceEntityConfiguration : IEntityTypeConfiguration<Device> {
    public void Configure(EntityTypeBuilder<Device> builder) {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DogId).IsRequired();
        builder.Property(d => d.MacAddress).IsRequired().HasMaxLength(17);
        builder.Property(d => d.IsConnected).IsRequired().HasDefaultValue(false);
        builder.HasOne(d => d.Dog).WithMany().HasForeignKey(d => d.DogId);
        builder.HasIndex(d => new {d.DogId, d.MacAddress}).IsUnique();
    }
}