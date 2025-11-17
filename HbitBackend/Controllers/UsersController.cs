using HbitBackend.Data;
using HbitBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HbitBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly PGDbContext _db;

    public UsersController(PGDbContext db)
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
                UserName = u.UserName,
                Email = u.Email,
                Name = u.Name,
                Surname = u.Surname,
                ActivitiesCount = u.Activities.Count()
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users
            .Where(u => u.Id == id)
            .Select(u => new UserGetDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Name = u.Name,
                Surname = u.Surname,
                ActivitiesCount = u.Activities.Count()
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound();
        return Ok(user);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Check uniqueness before insert to provide a friendly error
        if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName))
            return Conflict(new { field = "UserName", message = "UserName already exists." });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { field = "Email", message = "Email already exists." });

        var newUser = new User
        {
            UserName = dto.UserName!,
            Name = dto.Name!,
            Surname = dto.Surname!,
            Email = dto.Email!
        };

        _db.Users.Add(newUser);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Unique constraint violation - map to 409 Conflict
            return Conflict(new { message = "UserName or Email already exists." });
        }

        // Return created with id in body
        return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, new { id = newUser.Id }); 
    }
    
}