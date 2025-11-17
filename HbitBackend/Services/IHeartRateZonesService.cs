using HbitBackend.Models.HeartRateZones;

namespace HbitBackend.Services;

public interface IHeartRateZonesService
{
    /// <summary>
    /// Ensure that HeartRateZones for the given user are fresh (MaxHeartRate not older than maxAgeDays).
    /// If zones exist and are stale, compute new MaxHeartRate (using last month data) and update the entity in DB.
    /// Returns the HeartRateZones entity (possibly updated) or null if none exists for the user.
    /// </summary>
    Task<HeartRateZones?> EnsureFreshMaxHeartRateAsync(int userId, int maxAgeDays = 7);

    /// <summary>
    /// Compute max BPM for the last month for a given user (returns null if no samples).
    /// </summary>
    Task<int?> ComputeMaxHeartRateLastMonthAsync(int userId);
}
