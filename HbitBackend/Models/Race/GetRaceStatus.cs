namespace HbitBackend;

public class GetRaceStatus
{
    public int UsersPosition { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public bool IsRaceActive { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}