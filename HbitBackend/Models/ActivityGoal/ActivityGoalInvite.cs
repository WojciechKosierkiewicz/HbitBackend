namespace HbitBackend.Models.ActivityGoal;

public class ActivityGoalInvite
{
    public int Id { get; set; }

    // who sent invite
    public int FromUserId { get; set; }
    // who is invited
    public int ToUserId { get; set; }

    // target activity goal
    public int ActivityGoalId { get; set; }

    public ActivityGoalInviteStatus Status { get; set; } = ActivityGoalInviteStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }

    // navigation
    public HbitBackend.Models.User.User? FromUser { get; set; }
    public HbitBackend.Models.User.User? ToUser { get; set; }
    public ActivityGoal? ActivityGoal { get; set; }
}

public enum ActivityGoalInviteStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2
}

