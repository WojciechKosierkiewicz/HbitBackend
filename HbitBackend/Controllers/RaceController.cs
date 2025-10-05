using Microsoft.AspNetCore.Mvc;
using HbitBackend.Models;

namespace HbitBackend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RaceController : ControllerBase
{
    private readonly ILogger<RaceController> _logger;
    
    public RaceController(ILogger<RaceController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public GetRaceStatus GetRaceStatus(string userId)
    {
        return new GetRaceStatus
        {
            UsersPosition = int.TryParse(userId, out var position) ? position : 0,
            TimeRemaining = TimeSpan.FromMinutes(15),
            IsRaceActive = true,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddMinutes(10)
        };
    }
}