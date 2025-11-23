using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HbitBackend.Models.HeartRateSample;

namespace HbitBackend.Data.Configurations;

public class HeartRateSampleConfiguration : IEntityTypeConfiguration<HeartRateSample>
{
    public void Configure(EntityTypeBuilder<HeartRateSample> builder)
    {
        builder.HasOne(h => h.Activity)
               .WithMany(a => a.HeartRateSamples)
               .HasForeignKey(h => h.ActivityId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.ActivityId, h.Timestamp });
    }
}

