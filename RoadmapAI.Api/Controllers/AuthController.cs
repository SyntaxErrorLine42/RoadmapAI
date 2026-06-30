using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RoadmapAI.Api.Models;

namespace RoadmapAI.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<AppUser> userManager, IConfiguration config) : ControllerBase
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IConfiguration _config = config;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));
        var token = GenerateJwtToken(user);

        Response.Cookies.Append("roadmapai_auth", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(int.TryParse(_config["Jwt:ExpiryDays"], out var days) ? days : 7),
            Path = "/",
            IsEssential = true
        });

        return Ok(new { message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password" });

        // Generate JWT token for the authenticated user
        var token = GenerateJwtToken(user);

        var isHttps = HttpContext.Request.IsHttps;

        Response.Cookies.Append("roadmapai_auth", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(int.TryParse(_config["Jwt:ExpiryDays"], out var days) ? days : 7),
            IsEssential = true,
            Path = "/"
        });

        return Ok(new
        {
            user = new { user.Id, user.Email, user.DisplayName }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        return Ok(new { user = new { user.Id, user.Email, user.DisplayName } });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var isHttps = HttpContext.Request.IsHttps;

        Response.Cookies.Delete("roadmapai_auth", new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax
        });

        return Ok(new { message = "Logged out" });
    }

    private string GenerateJwtToken(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("displayName", user.DisplayName)
        };

        var secret = _config["Jwt:Secret"] ?? "a-very-long-and-secure-default-secret-key-32-chars-long";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out var days) ? days : 7;
        var expiry = DateTime.UtcNow.AddDays(expiryDays);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "RoadmapAI",
            audience: _config["Jwt:Audience"] ?? "RoadmapAI-Client",
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
