using System;

namespace HbitBackend.DTOs.ActivityGoal;

public class ActivityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime Date { get; set; }
    public int Duration { get; set; }
    public int Score { get; set; }
}

