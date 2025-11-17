using HbitBackend.Models.Activity;
using System.ComponentModel.DataAnnotations;

namespace HbitBackend.Models.ActivityGoal;

public class PostActivityGoalDto
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public DateTimeOffset StartsAt { get; set; }

    [Required]
    public DateTimeOffset EndsAt { get; set; }

    [Required]
    public ActivityGoalRange Range { get; set; }

    [Required]
    public int TargetValue { get; set; }

    public ICollection<ActivityType>? AcceptedActivityTypes { get; set; } = new List<ActivityType>(); 
}