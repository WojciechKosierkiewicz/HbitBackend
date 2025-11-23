using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HbitBackend.Models.ActivityGoalPoints;
using HbitBackend.Models.ActivityGoal;
using HbitBackend.Models.Activity;

namespace HbitBackend.Data.Configurations;

public class ActivityGoalPointsConfiguration : IEntityTypeConfiguration<ActivityGoalPoints>
{
    public void Configure(EntityTypeBuilder<ActivityGoalPoints> builder)
    {
        builder.HasIndex(p => new { p.ActivityId, p.ActivityGoalId });

        builder.HasOne(p => p.Activity)
               .WithMany()
               .HasForeignKey(p => p.ActivityId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ActivityGoal)
               .WithMany()
               .HasForeignKey(p => p.ActivityGoalId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

