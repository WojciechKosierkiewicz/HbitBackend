namespace HbitBackend.Models.HeartRateSample;

public class HeartRateSampleData
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int Bpm { get; set; }
}
