using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisionCheckAI.Server.Data;
using VisionCheckAI.Server.Models;
using VisionCheckAI.Server.Services;

namespace VisionCheckAI.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly VisionCheckDbContext _db;
    private readonly IAuthService _authService;

    public AuthController(VisionCheckDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required." });
        }

        var username = request.Username.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            // Auto-provision requested role for demo ease if user doesn't exist
            string role = "Inspector";
            if (username.Contains("admin", StringComparison.OrdinalIgnoreCase)) role = "Administrator";
            else if (username.Contains("super", StringComparison.OrdinalIgnoreCase)) role = "Supervisor";

            user = new Data.Entities.UserEntity
            {
                Username = username,
                PasswordHash = request.Password,
                DisplayName = username,
                Role = role
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var (token, expiresAt) = _authService.GenerateJwtToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role
            }
        });
    }
}
