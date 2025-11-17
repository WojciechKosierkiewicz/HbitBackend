namespace HbitBackend.Models;

public class Activity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public DateTime Date { get; set; }
    public ActivityType Type { get; set; }
    
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}