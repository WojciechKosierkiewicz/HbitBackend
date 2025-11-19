using HbitBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using HbitBackend.Services;

namespace HbitBackend.Controllers;

[ApiController] 
[Route("[controller]")]
public class ActivityPointsController : ControllerBase
{
    private readonly IActivityPointsService _pointsService;

    public ActivityPointsController(IActivityPointsService pointsService)
    {
        _pointsService = pointsService;
    }

    [HttpGet("{activityId}/{activityGoalId}")]
    [Authorize]
    public async Task<IActionResult> GetActivityPoints(int activityId, int activityGoalId)
    {
        if (!AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized(); 

        var points = await _pointsService.GetAtivityPoints(userId, activityId, activityGoalId);
        return Ok(new { points });
    }
}