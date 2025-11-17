using Microsoft.AspNetCore.Mvc;
using HbitBackend.Data;
using HbitBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace HbitBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class ActivityController : ControllerBase
{
    private readonly PGDbContext _db;

    public ActivityController(PGDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var activities = await _db.Activities.ToListAsync();
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
    public async Task<IActionResult> Create([FromBody] ActivityCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.UserId == null)
            return BadRequest(new { field = "UserId", message = "UserId is required." });

        var userExists = await _db.Users.AnyAsync(u => u.Id == dto.UserId);
        
        if (!userExists)
            return NotFound(new { field = "UserId", message = "User with given id was not found." });

        if (dto.ActivityType == null)
            return BadRequest(new { field = "ActivityType", message = "ActivityType is required." });

        if (dto.Date == null)
            return BadRequest(new { field = "Date", message = "Date is required." });

        var activityType = dto.ActivityType.Value;
        var date = dto.Date.Value;

        var newActivity = new Activity
        {
            Name = dto.Name,
            Date = date,
            Type = activityType,
            UserId = dto.UserId.Value 
        };

        _db.Activities.Add(newActivity);
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetById), new { id = newActivity.Id }, newActivity); 
    }
}