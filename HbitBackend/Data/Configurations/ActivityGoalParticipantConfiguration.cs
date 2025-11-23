using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HbitBackend.Models.ActivityGoal;
using HbitBackend.Models.User;

namespace HbitBackend.Data.Configurations;

public class ActivityGoalParticipantConfiguration : IEntityTypeConfiguration<ActivityGoalParticipant>
{
    public void Configure(EntityTypeBuilder<ActivityGoalParticipant> builder)
    {
        builder.HasKey(p => new { p.ActivityGoalId, p.UserId });

        builder.HasOne(p => p.ActivityGoal)
               .WithMany(g => g.Participants)
               .HasForeignKey(p => p.ActivityGoalId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
               .WithMany(u => u.ActivityGoals)
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

