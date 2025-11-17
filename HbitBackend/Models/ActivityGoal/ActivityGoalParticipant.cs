namespace HbitBackend.Models.ActivityGoal;

using HbitBackend.Models.User;

public class ActivityGoalParticipant
{
    public int ActivityGoalId { get; set; }
    public ActivityGoal ActivityGoal { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsOwner { get; set; } = false;
}
