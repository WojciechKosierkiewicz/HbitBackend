namespace HbitBackend.Models.HeartRateSample;

using HbitBackend.Models.Activity;

public class HeartRateSample
{
    public long Id { get; set; }        
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public int Bpm { get; set; }
}