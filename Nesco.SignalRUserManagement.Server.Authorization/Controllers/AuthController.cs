using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nesco.SignalRUserManagement.Server.Authorization.Models;
using Nesco.SignalRUserManagement.Server.Authorization.Options;

namespace Nesco.SignalRUserManagement.Server.Authorization.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController<TUser> : ControllerBase where TUser : IdentityUser
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly JwtAuthOptions? _jwtOptions;

    public AuthController(
        SignInManager<TUser> signInManager,
        UserManager<TUser> userManager,
        IConfiguration configuration,
        IOptions<JwtAuthOptions>? jwtOptions = null)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _configuration = configuration;
        _jwtOptions = jwtOptions?.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized("Invalid email or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var token = GenerateJwtToken(user);
            return Ok(new LoginResponse
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Token = token
            });
        }

        if (result.RequiresTwoFactor)
        {
            return Unauthorized("Two-factor authentication required");
        }

        if (result.IsLockedOut)
        {
            return Unauthorized("Account locked out");
        }

        return Unauthorized("Invalid email or password");
    }

    [HttpPost("logout")]
    [Authorize]
    public Task<IActionResult> Logout()
    {
        // For JWT, logout is handled client-side by discarding the token
        return Task.FromResult<IActionResult>(Ok());
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty
        });
    }

    private string GenerateJwtToken(TUser user)
    {
        // Use JwtAuthOptions if available, otherwise fall back to IConfiguration
        var jwtKey = _jwtOptions?.Key ?? _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        var jwtIssuer = _jwtOptions?.Issuer ?? _configuration["Jwt:Issuer"] ?? "UserManagementAndControl";
        var jwtAudience = _jwtOptions?.Audience ?? _configuration["Jwt:Audience"] ?? "UserManagementAndControl";
        var expirationDays = _jwtOptions?.TokenExpirationDays ?? 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Email ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
