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

    public async Task<int?> ComputeMaxHeartRateLastMonthAsync(int userId)
    {
        var since = DateTimeOffset.UtcNow.AddMonths(-1);

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

    public async Task<HeartRateZones?> EnsureFreshMaxHeartRateAsync(int userId, int maxAgeDays = 7)
    {
        var zones = await _db.HeartRateZones.FirstOrDefaultAsync(z => z.UserId == userId);
        if (zones == null) return null;

        if (!zones.IsStale(maxAgeDays)) return zones;

        var newMax = await ComputeMaxHeartRateLastMonthAsync(userId);

        var originalTimestamp = zones.Timestamp;
        var updated = await zones.RefreshMaxHeartRateIfStaleAsync(() => Task.FromResult(newMax), maxAgeDays);

        if (!updated && zones.Timestamp == originalTimestamp) return zones;
        
        _db.HeartRateZones.Update(zones);
        await _db.SaveChangesAsync();

        return zones;
    }
}
