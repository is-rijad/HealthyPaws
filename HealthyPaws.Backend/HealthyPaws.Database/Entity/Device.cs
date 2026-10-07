using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class Device
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public decimal CurrentWeight { get; set; }
    public bool IsConnected { get; set; }
    public Dog? Dog { get; set; }
}

public class DeviceEntityConfiguration : IEntityTypeConfiguration<Device> {
    public void Configure(EntityTypeBuilder<Device> builder) {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DogId).IsRequired();
        builder.Property(d => d.CurrentWeight).IsRequired();
        builder.Property(d => d.IsConnected).IsRequired().HasDefaultValue(false);
        builder.HasOne(d => d.Dog).WithMany(d => d.Devices).HasForeignKey(d => d.DogId);
    }
}