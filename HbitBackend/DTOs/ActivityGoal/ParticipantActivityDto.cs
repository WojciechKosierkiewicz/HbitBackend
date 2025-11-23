using System;

namespace HbitBackend.DTOs.ActivityGoal;

public class ParticipantActivityDto
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public int Rank { get; set; }
    public string ActivityName { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime Date { get; set; }
    public int Duration { get; set; }
    public int Score { get; set; }
}

