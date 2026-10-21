using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyPaws.Database.Entity;

public class UserDevice
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DeviceId { get; set; }

    public User? User { get; set; }
    public Device? Device { get; set; }
}

public class UserDeviceEntityConfiguration : IEntityTypeConfiguration<UserDevice> {
    public void Configure(EntityTypeBuilder<UserDevice> builder) {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserId).IsRequired();
        builder.Property(u => u.DeviceId).IsRequired();

        builder.HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId);
        builder.HasOne(u => u.Device).WithMany().HasForeignKey(u => u.DeviceId);

    }
}