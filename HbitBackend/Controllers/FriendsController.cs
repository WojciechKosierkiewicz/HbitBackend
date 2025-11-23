using HbitBackend.Data;
using HbitBackend.Models.Friend;
using HbitBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HbitBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class FriendsController : ControllerBase
{
    private readonly PgDbContext _db;

    public FriendsController(PgDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyFriends()
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId)) return Unauthorized();

        var friends = await _db.Friends
            .Where(f => f.UserAId == userId || f.UserBId == userId)
            .Select(f => new FriendDto { UserId = f.UserAId == userId ? f.UserBId : f.UserAId, Since = f.CreatedAt })
            .ToListAsync();

        return Ok(friends);
    }

    [HttpPost("requests")]
    [Authorize]
    public async Task<IActionResult> SendFriendRequest([FromBody] HbitBackend.Models.Friend.FriendRequestCreateDto dto)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId)) return Unauthorized();

        if (dto.ToUserId == userId) return BadRequest(new { message = "Cannot send request to yourself." });

        // check if already friends
        var already = await _db.Friends.AnyAsync(f => (f.UserAId == userId && f.UserBId == dto.ToUserId) || (f.UserAId == dto.ToUserId && f.UserBId == userId));
        if (already) return Conflict(new { message = "Already friends." });

        // check if request exists
        var exists = await _db.FriendRequests.AnyAsync(r => r.FromUserId == userId && r.ToUserId == dto.ToUserId && r.Status == HbitBackend.Models.Friend.FriendRequestStatus.Pending);
        if (exists) return Conflict(new { message = "Friend request already sent." });

        var req = new HbitBackend.Models.Friend.FriendRequest
        {
            FromUserId = userId,
            ToUserId = dto.ToUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = HbitBackend.Models.Friend.FriendRequestStatus.Pending
        };

        _db.FriendRequests.Add(req);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyFriendRequests), new { id = req.Id }, new { id = req.Id });
    }

    [HttpGet("requests")]
    [Authorize]
    public async Task<IActionResult> GetMyFriendRequests()
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId)) return Unauthorized();

        var incoming = await _db.FriendRequests
            .Where(r => r.ToUserId == userId && r.Status == FriendRequestStatus.Pending)
            .Select(r => new FriendRequestDto { Id = r.Id, FromUserId = r.FromUserId, ToUserId = r.ToUserId, CreatedAt = r.CreatedAt, Status = r.Status })
            .ToListAsync();

        return Ok(incoming);
    }

    [HttpPost("requests/{id}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptRequest(int id)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId)) return Unauthorized();

        var req = await _db.FriendRequests.FindAsync(id);
        if (req == null) return NotFound();
        if (req.ToUserId != userId) return Forbid();
        if (req.Status != HbitBackend.Models.Friend.FriendRequestStatus.Pending) return BadRequest(new { message = "Request already handled." });

        // create friend relation (ensure ordering to avoid duplicate keys)
        int from = req.FromUserId;
        int to = req.ToUserId;
        var a = Math.Min(from, to);
        var b = Math.Max(from, to);

        var friend = new HbitBackend.Models.Friend.Friend { UserAId = a, UserBId = b, CreatedAt = DateTimeOffset.UtcNow };
        _db.Friends.Add(friend);

        req.Status = HbitBackend.Models.Friend.FriendRequestStatus.Accepted;
        req.RespondedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("requests/{id}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectRequest(int id)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId)) return Unauthorized();

        var req = await _db.FriendRequests.FindAsync(id);
        if (req == null) return NotFound();
        if (req.ToUserId != userId) return Forbid();
        if (req.Status != HbitBackend.Models.Friend.FriendRequestStatus.Pending) return BadRequest(new { message = "Request already handled." });

        req.Status = HbitBackend.Models.Friend.FriendRequestStatus.Rejected;
        req.RespondedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{friendUserId}")]
    [Authorize]
    public async Task<IActionResult> RemoveFriend(int friendUserId)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId)) return Unauthorized();

        var a = Math.Min(userId, friendUserId);
        var b = Math.Max(userId, friendUserId);

        var friendship = await _db.Friends.FindAsync(a, b);
        if (friendship == null) return NotFound();

        _db.Friends.Remove(friendship);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
