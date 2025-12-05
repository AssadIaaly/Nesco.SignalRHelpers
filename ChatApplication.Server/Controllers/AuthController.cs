using ChatApplication.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nesco.SignalRUserManagement.Server.Authorization.Controllers;
using Nesco.SignalRUserManagement.Server.Authorization.Models;
using Nesco.SignalRUserManagement.Server.Authorization.Options;
using Nesco.SignalRUserManagement.Server.Services;

namespace ChatApplication.Server.Controllers;

public class AuthController : AuthController<ApplicationUser>
{
    private readonly ISignalRUserManagementService _userManagementService;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ISignalRUserManagementService userManagementService,
        IOptions<JwtAuthOptions>? jwtOptions = null)
        : base(signInManager, userManager, configuration, jwtOptions)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Check if a user is already connected from another client.
    /// Call this before login to determine if force logout is needed.
    /// </summary>
    [HttpPost("check-connection")]
    public async Task<ActionResult<CheckConnectionResponse>> CheckConnection([FromBody] LoginRequest request)
    {
        var user = await UserManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that user doesn't exist
            return Ok(new CheckConnectionResponse { IsConnected = false, ConnectionCount = 0 });
        }

        // Check password first
        var result = await SignInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            // Don't reveal connection status for invalid credentials
            return Ok(new CheckConnectionResponse { IsConnected = false, ConnectionCount = 0 });
        }

        var isConnected = _userManagementService.IsUserConnected(user.Id);
        var connections = await _userManagementService.GetConnectedUsersAsync();
        var userConnections = connections.Where(c => c.UserId == user.Id).ToList();

        return Ok(new CheckConnectionResponse
        {
            IsConnected = isConnected,
            ConnectionCount = userConnections.Sum(c => c.Connections.Count)
        });
    }
}

public record CheckConnectionResponse
{
    public bool IsConnected { get; init; }
    public int ConnectionCount { get; init; }
}
