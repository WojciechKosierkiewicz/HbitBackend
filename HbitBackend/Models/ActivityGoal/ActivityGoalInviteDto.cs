namespace HbitBackend.Models.ActivityGoal;

public class ActivityGoalInviteCreateDto
{
    public int ToUserId { get; set; }
    public int ActivityGoalId { get; set; }
}

public class ActivityGoalInviteDto
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public int ActivityGoalId { get; set; }
    public ActivityGoalInviteStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}

