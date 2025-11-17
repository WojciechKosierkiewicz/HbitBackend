using HbitBackend.Data;
using HbitBackend.Models.HeartRateSample;
using HbitBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using HbitBackend.Models.HeartRateZones;
using HbitBackend.Services;

namespace HbitBackend.Controllers;


[ApiController]
[Route("[controller]")]
public class HeartRateController : ControllerBase
{
    private readonly PgDbContext _db;
    private readonly IHeartRateZonesService _zonesService;
    
    public HeartRateController(PgDbContext db, IHeartRateZonesService zonesService)
    {
        _db = db;
        _zonesService = zonesService;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<HeartRateSampleData>>> GetHeartRateSample(int id)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized();

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return NotFound();
        if (activity.UserId != userId) return Forbid();

        var samples = await _db.HeartRateSamples
            .Where(h => h.ActivityId == id)
            .OrderBy(h => h.Timestamp)
            .Select(h => new HeartRateSampleData
            {
                Id = h.Id,
                Timestamp = h.Timestamp,
                Bpm = h.Bpm
            })
            .ToListAsync();

        return Ok(samples);
    }

    // POST endpoint przyjmujący tablicę próbek tętna dla danej aktywności
    [HttpPost("{id}")]
    [Authorize]
    public async Task<IActionResult> PostHeartRateSamples(int id, [FromBody] IEnumerable<HeartRateSampleCreateDto> samplesDto)
    {
        if (samplesDto == null || !samplesDto.Any())
            return BadRequest(new { message = "No samples provided." });

        if (!AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized();

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return NotFound();
        if (activity.UserId != userId) return Forbid();

        var entities = samplesDto.Select(s => new HeartRateSample
        {
            ActivityId = id,
            Timestamp = s.Timestamp,
            Bpm = s.Bpm
        }).ToList();

        _db.HeartRateSamples.AddRange(entities);
        await _db.SaveChangesAsync();

        var result = entities.Select(e => new HeartRateSampleData
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            Bpm = e.Bpm
        }).ToList();

        return CreatedAtAction(nameof(GetHeartRateSample), new { id = id }, result);
    }

    [Authorize]
    [HttpGet("zones")]
    public async Task<IActionResult> GetHeartRateZones()
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized();

        // Ensure zones are fresh - service may update DB
        await _zonesService.EnsureFreshMaxHeartRateAsync(userId);

        var zones = await _db.HeartRateZones.Where(h => h.UserId == userId).FirstOrDefaultAsync();
        if (zones == null) return NotFound();
        
        var response = new GetHeartRateZonesDto 
        {
            RestingHeartRate = zones.RestingHeartRate,
            MaxHeartRate = zones.MaxHeartRate,
            Zone1LowerLimit = zones.GetZoneMinBpm(1),
            Zone2LowerLimit =  zones.GetZoneMinBpm(2),
            Zone3LowerLimit =  zones.GetZoneMinBpm(3),
            Zone4LowerLimit =  zones.GetZoneMinBpm(4),
            Zone5LowerLimit =  zones.GetZoneMinBpm(5)
        };

        return Ok(response);
    }
}