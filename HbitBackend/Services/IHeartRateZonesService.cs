using HbitBackend.Models.HeartRateZones;

namespace HbitBackend.Services;

public interface IHeartRateZonesService
{
    /// <summary>
    /// Ensure that HeartRateZones for the given user are fresh (MaxHeartRate not older than maxAgeDays).
    /// If zones exist and are stale, compute new MaxHeartRate (using data from past year) and update the entity in DB.
    /// Returns the HeartRateZones entity (possibly updated) or null if none exists for the user.
    /// </summary>
    Task<HeartRateZones?> EnsureFreshMaxHeartRateAsync(int userId, int maxAgeDays = 7);

    /// <summary>
    /// Compute max BPM for the past year for a given user (returns null if no samples).
    /// </summary>
    Task<int?> ComputeMaxHeartRatePastYearAsync(int userId);

    /// <summary>
    /// Given a MaxHeartRate (in BPM) returns heart rate zones based on percent of Max HR.
    /// Zones lower limits are provided for Z1..Z5 according to: 50%,60%,70%,80%,90% of HRmax.
    /// </summary>
    GetHeartRateZonesDto ComputeZonesFromMax(int maxHeartRate);
}
