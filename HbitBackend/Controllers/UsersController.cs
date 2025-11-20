using HbitBackend.Data;
using HbitBackend.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HbitBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly PgDbContext _db;

    public UsersController(PgDbContext db)
    {
        _db = db;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .Select(u => new UserGetDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                Name = u.Name,
                Surname = u.Surname,
                ActivitiesCount = u.Activities.Count()
            })
            .ToListAsync();

        return Ok(users);
    }
    
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        if (!HbitBackend.Utils.AuthHelpers.TryGetUserId(User, out var userId))
            return Unauthorized();

        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserGetDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                Name = u.Name,
                Surname = u.Surname,
                ActivitiesCount = u.Activities.Count()
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users
            .Where(u => u.Id == id)
            .Select(u => new UserGetDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                Name = u.Name,
                Surname = u.Surname,
                ActivitiesCount = u.Activities.Count()
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound();
        return Ok(user);
    }
}