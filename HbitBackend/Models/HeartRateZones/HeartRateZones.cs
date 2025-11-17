using System.ComponentModel.DataAnnotations.Schema;

namespace HbitBackend.Models.HeartRateZones;

public class HeartRateZones
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int RestingHeartRate { get; set; }
    public int MaxHeartRate { get; set; }

    public int GetZoneMinBpm(int zone)
    {
        return zone switch
        {
            1 => (int)System.Math.Round(RestingHeartRate + 0.5 * (MaxHeartRate - RestingHeartRate)),
            2 => (int)System.Math.Round(RestingHeartRate + 0.6 * (MaxHeartRate - RestingHeartRate)),
            3 => (int)System.Math.Round(RestingHeartRate + 0.7 * (MaxHeartRate - RestingHeartRate)),
            4 => (int)System.Math.Round(RestingHeartRate + 0.8 * (MaxHeartRate - RestingHeartRate)),
            5 => (int)System.Math.Round(RestingHeartRate + 0.9 * (MaxHeartRate - RestingHeartRate)),
            _ => throw  new ArgumentOutOfRangeException(nameof(zone), zone, null)
        };
    }

    public bool IsStale(int maxAgeDays)
    {
        if (maxAgeDays < 0) return false;
        return (DateTimeOffset.UtcNow - Timestamp).TotalDays > maxAgeDays;
    }

    public async Task<bool> RefreshMaxHeartRateIfStaleAsync(Func<Task<int?>> computeMaxFunc, int maxAgeDays)
    {
        if (computeMaxFunc == null) throw new ArgumentNullException(nameof(computeMaxFunc));

        if (!IsStale(maxAgeDays)) return false;

        var newMax = await computeMaxFunc();
        // Zawsze odświeżamy timestamp, nawet gdy brak wyników, aby nie ponawiać zapytań zbyt często
        Timestamp = DateTimeOffset.UtcNow;

        if (!newMax.HasValue) return false;

        MaxHeartRate = newMax.Value;
        return true;
    }
}