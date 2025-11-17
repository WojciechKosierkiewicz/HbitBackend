using HbitBackend.Data;
using HbitBackend.Models.ActivityGoal;
using HbitBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetActivityGoals()
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId))
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
        if (!AuthHelpers.TryGetUserId(User, out var userId))
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
}