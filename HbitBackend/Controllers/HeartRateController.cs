using HbitBackend.Data;
using HbitBackend.Models.HeartRateSample;
using HbitBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> GetHeartRateSample(int id)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized();

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            return NotFound();
        if (activity.UserId != userId)
            return Forbid();

        var samples = await _db.HeartRateSamples
            .Where(h => h.ActivityId == id)
            .OrderBy(h => h.Timestamp)
            .Select(h => new {
                Time = h.Timestamp,
                Value = h.Bpm
            })
            .ToListAsync();

        return Ok(samples);
    }

    // POST endpoint przyjmujący tablicę próbek tętna dla danej aktywności
    [HttpPost("{id}")]
    [Authorize]
    public async Task<IActionResult> PostHeartRateSamples(int id, [FromBody] IEnumerable<HeartRateSampleCreateDto>? samplesDto)
    {
        var samplesList = samplesDto?.ToList();

        if (samplesList == null || !samplesList.Any())
            return BadRequest(new { message = "No samples provided." });

        if (!AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized();

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            return NotFound();

        if (activity.UserId != userId)
            return Forbid();

        var entities = samplesList.Select(s => new HeartRateSample
        {
            ActivityId = id,
            Timestamp = s.Timestamp,
            Bpm = s.Bpm
        }).ToList();

        _db.HeartRateSamples.AddRange(entities);
        await _db.SaveChangesAsync();

        var result = entities.Select(e => new {
            Time = e.Timestamp,
            Value = e.Bpm
        }).ToList();

        return CreatedAtAction(nameof(GetHeartRateSample), new { id }, result);
    }

    [Authorize]
    [HttpGet("zones")]
    public async Task<IActionResult> GetHeartRateZones()
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized();

        // Try to ensure DB entry is fresh (may update MaxHeartRate)
        var zonesEntity = await _zonesService.EnsureFreshMaxHeartRateAsync(userId);

        int? maxHr = zonesEntity?.MaxHeartRate;
        if (!maxHr.HasValue)
        {
            // fallback: compute directly from past year's samples (may use age-based fallback inside)
            maxHr = await _zonesService.ComputeMaxHeartRatePastYearAsync(userId);
        }

        if (!maxHr.HasValue)
            return NotFound(new { message = "Unable to determine max heart rate for user." });

        var zonesDto = _zonesService.ComputeZonesFromMax(maxHr.Value);
        // fill resting heart rate from DB if available
        zonesDto.RestingHeartRate = zonesEntity?.RestingHeartRate ?? 0;

        return Ok(zonesDto);
    }
}