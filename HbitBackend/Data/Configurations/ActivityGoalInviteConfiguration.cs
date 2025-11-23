using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HbitBackend.Models.ActivityGoal;

namespace HbitBackend.Data.Configurations;

public class ActivityGoalInviteConfiguration : IEntityTypeConfiguration<ActivityGoalInvite>
{
    public void Configure(EntityTypeBuilder<ActivityGoalInvite> builder)
    {
        builder.HasOne(i => i.FromUser)
               .WithMany()
               .HasForeignKey(i => i.FromUserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ToUser)
               .WithMany()
               .HasForeignKey(i => i.ToUserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ActivityGoal)
               .WithMany()
               .HasForeignKey(i => i.ActivityGoalId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

