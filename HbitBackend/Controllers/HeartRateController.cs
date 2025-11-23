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
        if (!AuthHelpers.TryGetUserId(User, out _))
            return Unauthorized();

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            return NotFound();
        // allow any authenticated user to read samples for any activity (no ownership check)

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

    [HttpGet("zones")]
    [Authorize]
    public async Task<IActionResult> GetHeartRateZones([FromQuery] int? userId = null)
    {
        if (!AuthHelpers.TryGetUserId(User, out var requesterId))
            return Unauthorized();
        
        // allow requester to optionally specify which user's zones to fetch; default to self
        var targetUserId = userId ?? requesterId;

        // Try to ensure DB entry is fresh for target user (may update MaxHeartRate)
        var zonesEntity = await _zonesService.EnsureFreshMaxHeartRateAsync(targetUserId);

        int? maxHr = zonesEntity?.MaxHeartRate;
        if (!maxHr.HasValue)
        {
            // fallback: compute directly from past year's samples (may use age-based fallback inside)
            maxHr = await _zonesService.ComputeMaxHeartRatePastYearAsync(targetUserId);
        }

        if (!maxHr.HasValue)
            return NotFound(new { message = "Unable to determine max heart rate for user." });

        var zonesDto = _zonesService.ComputeZonesFromMax(maxHr.Value);
        // fill resting heart rate from DB if available
        zonesDto.RestingHeartRate = zonesEntity?.RestingHeartRate ?? 0;

        return Ok(zonesDto);
    }

    [Authorize]
    [HttpGet("{id}/zones/timespent")]
    public async Task<IActionResult> GetTimeSpentInZonesForActivity(int id, [FromQuery] int days = 30)
    {
        if (!AuthHelpers.TryGetUserId(User, out _))
            return Unauthorized();

        // validate activity exists (but do not enforce ownership) - allow reading others' activity times
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return NotFound(new { message = "Activity not found.", activityId = id });

        // Determine max HR via service (ensure freshness first)
        var zonesEntity = await _zonesService.EnsureFreshMaxHeartRateAsync(activity.UserId);
        int? maxHr = zonesEntity?.MaxHeartRate;
        if (!maxHr.HasValue)
            maxHr = await _zonesService.ComputeMaxHeartRatePastYearAsync(activity.UserId);

        if (!maxHr.HasValue)
            return NotFound(new { message = "Unable to determine max heart rate for user." });

        var zonesDto = _zonesService.ComputeZonesFromMax(maxHr.Value);

        // build zone lower thresholds
        var z2 = zonesDto.Zone2LowerLimit;
        var z3 = zonesDto.Zone3LowerLimit;
        var z4 = zonesDto.Zone4LowerLimit;
        var z5 = zonesDto.Zone5LowerLimit;
        var max = zonesDto.MaxHeartRate;

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        // fetch samples for the specified activity only
        var samples = await _db.HeartRateSamples
            .Where(h => h.ActivityId == id && h.Timestamp >= since)
            .OrderBy(h => h.Timestamp)
            .Select(h => new { h.Timestamp, h.Bpm })
            .ToListAsync();

        var zoneSeconds = new Dictionary<int, double> { {1,0}, {2,0}, {3,0}, {4,0}, {5,0} };

        if (samples.Count <= 1)
        {
            // nothing to sum, return zeros
            var emptyResult = new [] {
                new { Zone = "Z1", Seconds = 0, Duration = "00:00:00" },
                new { Zone = "Z2", Seconds = 0, Duration = "00:00:00" },
                new { Zone = "Z3", Seconds = 0, Duration = "00:00:00" },
                new { Zone = "Z4", Seconds = 0, Duration = "00:00:00" },
                new { Zone = "Z5", Seconds = 0, Duration = "00:00:00" }
            };
            return Ok(new { MaxHeartRate = max, Zones = emptyResult });
        }

        for (int i = 0; i < samples.Count - 1; i++)
        {
            var cur = samples[i];
            var next = samples[i+1];
            var delta = (next.Timestamp - cur.Timestamp).TotalSeconds;
            if (delta <= 0) continue;

            int zone;
            var bpm = cur.Bpm;
            if (bpm >= z5) zone = 5;
            else if (bpm >= z4) zone = 4;
            else if (bpm >= z3) zone = 3;
            else if (bpm >= z2) zone = 2;
            else zone = 1;

            zoneSeconds[zone] += delta;
        }

        var result = new [] {
            new { Zone = "Z1", Seconds = (int)zoneSeconds[1], Duration = TimeSpan.FromSeconds(zoneSeconds[1]).ToString() },
            new { Zone = "Z2", Seconds = (int)zoneSeconds[2], Duration = TimeSpan.FromSeconds(zoneSeconds[2]).ToString() },
            new { Zone = "Z3", Seconds = (int)zoneSeconds[3], Duration = TimeSpan.FromSeconds(zoneSeconds[3]).ToString() },
            new { Zone = "Z4", Seconds = (int)zoneSeconds[4], Duration = TimeSpan.FromSeconds(zoneSeconds[4]).ToString() },
            new { Zone = "Z5", Seconds = (int)zoneSeconds[5], Duration = TimeSpan.FromSeconds(zoneSeconds[5]).ToString() }
        };

        return Ok(new { MaxHeartRate = max, Zones = result });
    }
}