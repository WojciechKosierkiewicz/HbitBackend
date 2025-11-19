using HbitBackend.Data;
using HbitBackend.Models.Activity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HbitBackend.Services;

public class ActivityPointsService : IActivityPointsService
{
    private readonly PgDbContext _db;

    public ActivityPointsService(PgDbContext db)
    {
        _db = db;
    }
    
    private async Task<IEnumerable<Activity>> FindSimilarActivities(int userId, Activity activity,DateTimeOffset timeWindowStart)
    {
        var activityType = activity.Type;

        var similarActivity = await _db.Activities
            .Where(a => a.UserId == userId &&
                        a.Type == activityType &&
                        a.Date >= timeWindowStart)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
        
        return similarActivity;
    }

    private async Task<int> CalculateActivityPoints(int userId, int activityId)
    {
        var activity = await _db.Activities.Where(a => a.UserId == userId && a.Id == activityId).FirstAsync();
        var timeWindowStart = activity.Date.AddDays(-30);
        
        var similarActivities = await FindSimilarActivities(userId, activity, timeWindowStart);
        while (timeWindowStart > activity.Date.AddYears(-1) && similarActivities.Count() < 5)
        {
            timeWindowStart = timeWindowStart.AddDays(-30);
            similarActivities = await FindSimilarActivities(userId, activity, timeWindowStart);
        }

        return 0;
    }
    
    public async Task<int> CalculateBonusPointsFromBpm(int userId, int activityId, int windowMinutes = 15)
    {
        if (windowMinutes < 0) windowMinutes = 15;

        var reference = await _db.Activities.FirstOrDefaultAsync(a => a.Id == activityId && a.UserId == userId);
        if (reference == null) return 0;

        var now = DateTime.UtcNow;
        var monthAgo = now.AddDays(-30);

        var candidates = await _db.Activities
            .Where(a => a.UserId == userId && a.Type == reference.Type && a.Date >= monthAgo && a.Date <= now)
            .ToListAsync();

        var targetTime = reference.Date.TimeOfDay;

        var matched = candidates.Where(a => Math.Abs((a.Date.TimeOfDay - targetTime).TotalMinutes) <= windowMinutes).ToList();

        // return count for now; later we can convert this to a points formula
        return matched.Count;
    }


    public async Task<int> GetAtivityPoints(int userId, int activityId, int activityGoalId)
    
    {
        // if there is already a record for this activity and goal return it
        var existingRecord = await _db.ActivityGoalPoints
            .FirstOrDefaultAsync(ap => ap.ActivityId == activityId && ap.ActivityGoalId == activityGoalId);

        if (existingRecord != null)
            return existingRecord.Points;

        return 0;
    }
}
