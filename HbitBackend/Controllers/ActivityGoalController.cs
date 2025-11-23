using System.Security.Claims;
using HbitBackend.Data;
using HbitBackend.Models.ActivityGoal;
using HbitBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HbitBackend.DTOs.ActivityGoal;

namespace HbitBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class ActivityGoalController : ControllerBase
{
    private readonly PgDbContext _db; 

    public ActivityGoalController(PgDbContext db)
    {
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        if (!AuthHelpers.TryGetUserId(User, out var id)) return false;
        userId = id;
        return true;
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetActivityGoals()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        
        var goals = await _db.ActivityGoalParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Include(p => p.ActivityGoal)
            .Select(p => new
            {
                p.ActivityGoal.Id,
                p.ActivityGoal.Name,
                p.ActivityGoal.Description,
                p.ActivityGoal.TargetValue,
                p.ActivityGoal.Range,
                p.ActivityGoal.AcceptedActivityTypes,
                p.ActivityGoal.StartsAt,
                p.ActivityGoal.EndsAt
            })
            .ToListAsync();
        
        return Ok(goals);
    }
    

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateActivityGoal([FromBody] PostActivityGoalDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var activityGoal = new ActivityGoal
        {
            Name = dto.Name,
            Description =  dto.Description,
            TargetValue = dto.TargetValue,
            Range = dto.Range,
            AcceptedActivityTypes =  dto.AcceptedActivityTypes,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
        };

        // add goal and participant (owner) and save in one operation
        _db.ActivityGoals.Add(activityGoal);

        var participant = new ActivityGoalParticipant
        {
            UserId = userId,
            ActivityGoal = activityGoal,
            IsOwner = true,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _db.ActivityGoalParticipants.Add(participant);

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetActivityGoals), new { id = activityGoal.Id }, new { id = activityGoal.Id });
    }

    // --- NEW ENDPOINTS FOR GOAL DETAIL VIEW ---

    [HttpGet("{goalId}/leaderboard")]
    [Authorize]
    public async Task<IActionResult> Leaderboard(int goalId)
    {
        if (!TryGetUserId(out var currentUserId)) return Unauthorized();

        var goal = await _db.ActivityGoals.FindAsync(goalId);
        if (goal == null) return NotFound();

        // totals per user for this goal
        var totals = await _db.ActivityGoalPoints
            .Where(p => p.ActivityGoalId == goalId)
            .Join(_db.Activities, p => p.ActivityId, a => a.Id, (p, a) => new { a.UserId, p.Points })
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Score = g.Sum(x => x.Points) })
            .OrderByDescending(x => x.Score)
            .ToListAsync();

        if (!totals.Any()) return Ok(new List<LeaderboardItemDto>());

        var ranked = totals.Select((t, idx) => new { t.UserId, t.Score, Rank = idx + 1 }).ToList();

        // preload user display names
        var userIds = ranked.Select(r => r.UserId).ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name, u.UserName })
            .ToDictionaryAsync(u => u.Id, u => (Name: u.Name, Username: u.UserName));

        var meIndex = ranked.FindIndex(r => r.UserId == currentUserId);
        var result = new List<LeaderboardItemDto>();

        if (meIndex >= 0)
        {
            if (meIndex - 1 >= 0) result.Add(MapRanked(ranked[meIndex - 1], users, false));
            result.Add(MapRanked(ranked[meIndex], users, true));
            if (meIndex + 1 < ranked.Count) result.Add(MapRanked(ranked[meIndex + 1], users, false));
        }
        else
        {
            // return top 3
            foreach (var r in ranked.Take(3)) result.Add(MapRanked(r, users, r.UserId == currentUserId));
        }

        return Ok(result);
    }

    private LeaderboardItemDto MapRanked(dynamic rankedEntry, Dictionary<int, (string? Name, string? Username)> users, bool isCurrent = false)
    {
        int uid = (int)rankedEntry.UserId;
        string display = "Unknown";
        if (users.TryGetValue(uid, out var u)) display = string.IsNullOrEmpty(u.Name) ? u.Username ?? "Unknown" : u.Name!;

        return new LeaderboardItemDto
        {
            Rank = (int)rankedEntry.Rank,
            UserName = isCurrent ? "You" : display,
            Score = (int)rankedEntry.Score,
            IsCurrentUser = isCurrent
        };
    }

    [HttpGet("{goalId}/fulfillments")]
    [Authorize]
    public async Task<IActionResult> Fulfillments(int goalId)
    {
        if (!TryGetUserId(out var currentUserId)) return Unauthorized();

        var goal = await _db.ActivityGoals.FindAsync(goalId);
        if (goal == null) return NotFound();

        var range = goal.Range;
        int periods = range switch
        {
            ActivityGoalRange.Daily => 14,
            ActivityGoalRange.Weekly => 12,
            ActivityGoalRange.Monthly => 6,
            ActivityGoalRange.Yearly => 12,
            _ => 14
        };

        var now = DateTime.UtcNow;
        var results = new List<FulfillmentDto>();

        for (int i = 0; i < periods; i++)
        {
            (DateTime start, DateTime end) = range switch
            {
                ActivityGoalRange.Daily => (now.Date.AddDays(-i), now.Date.AddDays(-i).AddDays(1)),
                ActivityGoalRange.Weekly => (now.Date.AddDays(-7 * (i + 1) + 1), now.Date.AddDays(-7 * i + 1)),
                ActivityGoalRange.Monthly => (new DateTime(now.Year, now.Month, 1).AddMonths(-i), new DateTime(now.Year, now.Month, 1).AddMonths(-i + 1)),
                ActivityGoalRange.Yearly => (new DateTime(now.Year - i, 1, 1), new DateTime(now.Year - i + 1, 1, 1)),
                _ => (now.Date.AddDays(-i), now.Date.AddDays(-i).AddDays(1))
            };

            // Ensure DateTimes are explicitly marked as UTC so Npgsql can map them to timestamptz
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

            // respect goal bounds
            if (goal.StartsAt.HasValue && end < goal.StartsAt.Value.UtcDateTime) break;
            if (goal.EndsAt.HasValue && start > goal.EndsAt.Value.UtcDateTime) continue;

            var pointsInPeriod = await _db.ActivityGoalPoints
                .Where(p => p.ActivityGoalId == goalId)
                .Join(_db.Activities, p => p.ActivityId, a => a.Id, (p, a) => new { a.UserId, p.Points, a.Date })
                .Where(x => x.UserId == currentUserId && x.Date >= start && x.Date < end)
                .SumAsync(x => (int?)x.Points) ?? 0;

            results.Add(new FulfillmentDto { Date = start.ToUniversalTime(), IsFulfilled = pointsInPeriod >= goal.TargetValue });
        }

        return Ok(results);
    }

    [HttpGet("{goalId}/activities")]
    [Authorize]
    public async Task<IActionResult> GoalActivities(int goalId)
    {
        if (!TryGetUserId(out var currentUserId)) return Unauthorized();

        var exists = await _db.ActivityGoals.AnyAsync(g => g.Id == goalId);
        if (!exists) return NotFound();

        var activities = await _db.ActivityGoalPoints
            .Where(p => p.ActivityGoalId == goalId)
            .Join(_db.Activities, p => p.ActivityId, a => a.Id, (p, a) => new { a.Id, a.Name, a.Type, a.Date, a.UserId, Points = p.Points })
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        var activityIds = activities.Select(a => a.Id).ToList();

        var durations = await _db.HeartRateSamples
            .Where(h => activityIds.Contains(h.ActivityId))
            .GroupBy(h => h.ActivityId)
            .Select(g => new { ActivityId = g.Key, Duration = (int)(g.Max(x => x.Timestamp).UtcDateTime - g.Min(x => x.Timestamp).UtcDateTime).TotalSeconds })
            .ToDictionaryAsync(x => x.ActivityId, x => x.Duration);

        var dto = activities.Select(a => new ActivityDto
        {
            Id = a.Id,
            Name = a.Name,
            Type = a.Type.ToString(),
            Date = a.Date,
            Duration = durations.ContainsKey(a.Id) ? durations[a.Id] : 0,
            Score = a.Points
        }).ToList();

        return Ok(dto);
    }

    [HttpGet("{goalId}/participants/activities")]
    [Authorize]
    public async Task<IActionResult> ParticipantActivities(int goalId)
    {
        var exists = await _db.ActivityGoals.AnyAsync(g => g.Id == goalId);
        if (!exists) return NotFound();

        // user totals for rank
        var userTotals = await _db.ActivityGoalPoints
            .Where(p => p.ActivityGoalId == goalId)
            .Join(_db.Activities, p => p.ActivityId, a => a.Id, (p, a) => new { a.UserId, p.Points })
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Score = g.Sum(x => x.Points) })
            .OrderByDescending(x => x.Score)
            .ToListAsync();

        var ranks = userTotals.Select((u, idx) => new { u.UserId, u.Score, Rank = idx + 1 }).ToDictionary(x => x.UserId, x => x.Rank);

        var activities = await _db.ActivityGoalPoints
            .Where(p => p.ActivityGoalId == goalId)
            .Join(_db.Activities, p => p.ActivityId, a => a.Id, (p, a) => new { a.Id, a.UserId, a.Name, a.Type, a.Date, Points = p.Points })
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        var activityIds = activities.Select(a => a.Id).ToList();
        var durations = await _db.HeartRateSamples
            .Where(h => activityIds.Contains(h.ActivityId))
            .GroupBy(h => h.ActivityId)
            .Select(g => new { ActivityId = g.Key, Duration = (int)(g.Max(x => x.Timestamp).UtcDateTime - g.Min(x => x.Timestamp).UtcDateTime).TotalSeconds })
            .ToDictionaryAsync(x => x.ActivityId, x => x.Duration);

        // preload users
        var userIds = activities.Select(a => a.UserId).Distinct().ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Name, u.UserName }).ToDictionaryAsync(u => u.Id, u => (Name: u.Name, Username: u.UserName));

        var result = activities.Select(a => new ParticipantActivityDto
        {
            Id = a.Id.ToString(),
            UserName = users.TryGetValue(a.UserId, out var u) ? (string.IsNullOrEmpty(u.Name) ? u.Username ?? "Unknown" : u.Name) : "Unknown",
            Rank = ranks.ContainsKey(a.UserId) ? ranks[a.UserId] : 0,
            ActivityName = a.Name,
            Type = a.Type.ToString(),
            Date = a.Date,
            Duration = durations.ContainsKey(a.Id) ? durations[a.Id] : 0,
            Score = a.Points
        }).ToList();

        return Ok(result);
    }

    // ActivityGoal invites
    [HttpPost("invites")]
    [Authorize]
    public async Task<IActionResult> CreateInvite([FromBody] ActivityGoalInviteCreateDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        // check that activity goal exists
        var goal = await _db.ActivityGoals.FindAsync(dto.ActivityGoalId);
        if (goal == null) return NotFound(new { message = "ActivityGoal not found." });

        // requester must be owner/participant with IsOwner = true
        var isOwner = await _db.ActivityGoalParticipants.AnyAsync(p => p.ActivityGoalId == dto.ActivityGoalId && p.UserId == userId && p.IsOwner);
        if (!isOwner) return Forbid();

        if (dto.ToUserId == userId) return BadRequest(new { message = "Cannot invite yourself." });

        // check existing invite
        var exists = await _db.ActivityGoalInvites.AnyAsync(i => i.ActivityGoalId == dto.ActivityGoalId && i.FromUserId == userId && i.ToUserId == dto.ToUserId && i.Status == ActivityGoalInviteStatus.Pending);
        if (exists) return Conflict(new { message = "Invite already sent." });

        var invite = new ActivityGoalInvite
        {
            ActivityGoalId = dto.ActivityGoalId,
            FromUserId = userId,
            ToUserId = dto.ToUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = ActivityGoalInviteStatus.Pending
        };

        _db.ActivityGoalInvites.Add(invite);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyInvites), new { id = invite.Id }, new { id = invite.Id });
    }

    [HttpGet("invites")]
    [Authorize]
    public async Task<IActionResult> GetMyInvites()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var invites = await _db.ActivityGoalInvites
            .Where(i => i.ToUserId == userId)
            .Select(i => new ActivityGoalInviteDto { Id = i.Id, FromUserId = i.FromUserId, ToUserId = i.ToUserId, ActivityGoalId = i.ActivityGoalId, Status = i.Status, CreatedAt = i.CreatedAt, RespondedAt = i.RespondedAt })
            .ToListAsync();

        return Ok(invites);
    }

    [HttpPost("invites/{id}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptInvite(int id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var invite = await _db.ActivityGoalInvites.FindAsync(id);
        if (invite == null) return NotFound();
        if (invite.ToUserId != userId) return Forbid();
        if (invite.Status != ActivityGoalInviteStatus.Pending) return BadRequest(new { message = "Invite already handled." });

        // add participant
        var participantExists = await _db.ActivityGoalParticipants.AnyAsync(p => p.ActivityGoalId == invite.ActivityGoalId && p.UserId == userId);
        if (!participantExists)
        {
            var participant = new ActivityGoalParticipant { ActivityGoalId = invite.ActivityGoalId, UserId = userId, IsOwner = false, JoinedAt = DateTimeOffset.UtcNow };
            _db.ActivityGoalParticipants.Add(participant);
        }

        invite.Status = ActivityGoalInviteStatus.Accepted;
        invite.RespondedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("invites/{id}/decline")]
    [Authorize]
    public async Task<IActionResult> DeclineInvite(int id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var invite = await _db.ActivityGoalInvites.FindAsync(id);
        if (invite == null) return NotFound();
        if (invite.ToUserId != userId) return Forbid();
        if (invite.Status != ActivityGoalInviteStatus.Pending) return BadRequest(new { message = "Invite already handled." });

        invite.Status = ActivityGoalInviteStatus.Declined;
        invite.RespondedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}