namespace HbitBackend.Models.HeartRateSample;

public class HeartRateSampleCreateDto
{
    public DateTimeOffset Timestamp { get; set; }
    public int Bpm { get; set; }
}
