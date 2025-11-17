using HbitBackend.Models.User;
using HbitBackend.Models.Activity;

namespace HbitBackend.Models.ActivityGoal;

public class ActivityGoal
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ActivityGoalRange Range { get; set;}
    public int TargetValue { get; set; }
    public ICollection<ActivityType>? AcceptedActivityTypes { get; set; } = new List<ActivityType>();
    public ICollection<ActivityGoalParticipant> Participants { get; set; } = new List<ActivityGoalParticipant>();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}