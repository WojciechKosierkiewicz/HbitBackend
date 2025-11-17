using Microsoft.AspNetCore.Mvc;
using HbitBackend.Data;
using HbitBackend.Models;
using HbitBackend.Models.Activity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HbitBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class ActivityController : ControllerBase
{
    private readonly PgDbContext _db;

    public ActivityController(PgDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var activities = await _db.Activities.Where(a => a.UserId == userId).ToListAsync();
        return Ok(activities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return NotFound();
        return Ok(activity);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ActivityCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        if (dto.ActivityType == null)
            return BadRequest(new { field = "ActivityType", message = "ActivityType is required." });

        if (dto.Date == null)
            return BadRequest(new { field = "Date", message = "Date is required." });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var activityType = dto.ActivityType.Value;
        var date = dto.Date.Value;

        var newActivity = new Activity
        {
            Name = dto.Name,
            Date = date,
            Type = activityType,
            UserId = userId
        };

        _db.Activities.Add(newActivity);
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetById), new { id = newActivity.Id }, newActivity); 
    }
}