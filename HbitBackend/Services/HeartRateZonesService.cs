using HbitBackend.Data;
using HbitBackend.Models.HeartRateZones;
using Microsoft.EntityFrameworkCore;

namespace HbitBackend.Services;

public class HeartRateZonesService : IHeartRateZonesService
{
    private readonly PgDbContext _db;

    public HeartRateZonesService(PgDbContext db)
    {
        _db = db;
    }

    public async Task<int?> ComputeMaxHeartRatePastYearAsync(int userId)
    {
        var since = DateTimeOffset.UtcNow.AddYears(-1);

        var maxBpm = await (from h in _db.HeartRateSamples
                            join a in _db.Activities on h.ActivityId equals a.Id
                            where a.UserId == userId && a.Date >= since
                            select (int?)h.Bpm)
                           .MaxAsync();

        if (maxBpm == null)
        {
            var usersDateOfBirth = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.DateOfBirth)
                .FirstOrDefaultAsync();
            
            var usersage = usersDateOfBirth.HasValue 
                ? (int)((DateTimeOffset.UtcNow - usersDateOfBirth.Value).TotalDays / 365.25)
                : (int?)null;

            if (usersage.HasValue)
            {
                maxBpm = (int?)(211 - (0.64 * usersage.Value));
            }
        }

        return maxBpm;
    }

    public GetHeartRateZonesDto ComputeZonesFromMax(int maxHeartRate)
    {
        // lower limits for each zone: Z1=50%, Z2=60%, Z3=70%, Z4=80%, Z5=90%
        int z1 = (int)Math.Round(maxHeartRate * 0.50);
        int z2 = (int)Math.Round(maxHeartRate * 0.60);
        int z3 = (int)Math.Round(maxHeartRate * 0.70);
        int z4 = (int)Math.Round(maxHeartRate * 0.80);
        int z5 = (int)Math.Round(maxHeartRate * 0.90);

        return new GetHeartRateZonesDto
        {
            RestingHeartRate = 0, // unknown here; caller can fill if needed
            MaxHeartRate = maxHeartRate,
            Zone1LowerLimit = z1,
            Zone2LowerLimit = z2,
            Zone3LowerLimit = z3,
            Zone4LowerLimit = z4,
            Zone5LowerLimit = z5
        };
    }

    public async Task<HeartRateZones?> EnsureFreshMaxHeartRateAsync(int userId, int maxAgeDays = 7)
    {
        var zones = await _db.HeartRateZones.FirstOrDefaultAsync(z => z.UserId == userId);
        if (zones == null) return null;

        if (!zones.IsStale(maxAgeDays)) return zones;

        var newMax = await ComputeMaxHeartRatePastYearAsync(userId);

        var originalTimestamp = zones.Timestamp;
        var updated = await zones.RefreshMaxHeartRateIfStaleAsync(() => Task.FromResult(newMax), maxAgeDays);

        if (!updated && zones.Timestamp == originalTimestamp) return zones;
        
        // update persisted max heart rate
        if (newMax.HasValue)
        {
            zones.MaxHeartRate = newMax.Value;
        }
        zones.Timestamp = DateTimeOffset.UtcNow;

        _db.HeartRateZones.Update(zones);
        await _db.SaveChangesAsync();

        return zones;
    }
}
