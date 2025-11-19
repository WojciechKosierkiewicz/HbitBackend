namespace HbitBackend.Models.ActivityGoalPoints;

using HbitBackend.Models.Activity;
using HbitBackend.Models.ActivityGoal;

public class ActivityGoalPoints
{
    public int Id { get; set; }
    
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    
    public int ActivityGoalId { get; set; }
    public ActivityGoal ActivityGoal { get; set; } = null!;
    
    public int Points { get; set; }
}