using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HbitBackend.Models.HeartRateZones;
using HbitBackend.Models.User;

namespace HbitBackend.Data.Configurations;

public class HeartRateZonesConfiguration : IEntityTypeConfiguration<HeartRateZones>
{
    public void Configure(EntityTypeBuilder<HeartRateZones> builder)
    {
        builder.HasOne<User>()
               .WithOne(u => u.HeartRateZones)
               .HasForeignKey<HeartRateZones>(h => h.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.UserId).IsUnique();
    }
}

