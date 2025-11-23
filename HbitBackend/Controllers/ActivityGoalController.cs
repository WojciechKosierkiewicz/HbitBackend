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

    // ActivityGoal invites
    [HttpPost("invites")]
    [Authorize]
    public async Task<IActionResult> CreateInvite([FromBody] ActivityGoalInviteCreateDto dto)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId))
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
        if (!AuthHelpers.TryGetUserId(User, out var userId))
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
        if (!AuthHelpers.TryGetUserId(User, out var userId))
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
        if (!AuthHelpers.TryGetUserId(User, out var userId))
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