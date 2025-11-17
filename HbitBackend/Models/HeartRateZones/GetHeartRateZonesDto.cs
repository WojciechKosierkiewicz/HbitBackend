namespace HbitBackend.Models.HeartRateZones;

public class GetHeartRateZonesDto
{
    public int RestingHeartRate { get; set; }
    public int MaxHeartRate { get; set; }
    public int Zone1LowerLimit { get; set; }
    public int Zone2LowerLimit { get; set; }
    public int Zone3LowerLimit { get; set; }
    public int Zone4LowerLimit { get; set; }
    public int Zone5LowerLimit { get; set; }
}
