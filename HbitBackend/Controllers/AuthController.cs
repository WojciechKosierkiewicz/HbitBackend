using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HbitBackend.Models.User;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HbitBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserManager<User> userManager, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingByUser = await _userManager.FindByNameAsync(dto.UserName!);
        if (existingByUser != null) return Conflict(new { field = "UserName", message = "UserName already exists." });

        var existingByEmail = await _userManager.FindByEmailAsync(dto.Email!);
        if (existingByEmail != null) return Conflict(new { field = "Email", message = "Email already exists." });

        var user = new User
        {
            UserName = dto.UserName!,
            Email = dto.Email!,
            Name = dto.Name ?? string.Empty,
            Surname = dto.Surname ?? string.Empty,
            DateOfBirth = dto.DateOfBirth ?? default
        };

        var result = await _userManager.CreateAsync(user, dto.Password ?? "password");
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create account for userName={UserName}; errors={Errors}", dto.UserName, result.Errors);
            return BadRequest(result.Errors);
        }

        // Log created account data (do NOT log password)
        _logger.LogInformation("Account created: {@Account}", new {
            user.Id,
            user.UserName,
            user.Email,
            user.Name,
            user.Surname,
            user.DateOfBirth
        });

        return CreatedAtAction(null, new { id = user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.UserName!);
        if (user == null) return Unauthorized(new { message = "Invalid username or password." });

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password!);
        if (!passwordValid) return Unauthorized(new { message = "Invalid username or password." });

        var token = GenerateJwtToken(user);
        return Ok(token);
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Capture data to log before deletion
        var userData = new { user.Id, user.UserName, user.Email, user.Name, user.Surname };

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to delete account for userId={UserId}; errors={Errors}", user.Id, result.Errors);
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("Account removed: {@Account}", userData);
        return NoContent();
    }

    private object GenerateJwtToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"]!;
        var jwtIssuer = jwtSection["Issuer"]!;
        var jwtAudience = jwtSection["Audience"]!;
        var expiresMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        var jwt = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new { access_token = token, expires_in = expiresMinutes * 60 };
    }
}
