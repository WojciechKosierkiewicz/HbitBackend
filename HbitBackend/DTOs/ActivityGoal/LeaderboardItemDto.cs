namespace HbitBackend.DTOs.ActivityGoal;

public class LeaderboardItemDto
{
    public int Rank { get; set; }
    public string UserName { get; set; } = "";
    public int Score { get; set; }
    public bool IsCurrentUser { get; set; }
}

