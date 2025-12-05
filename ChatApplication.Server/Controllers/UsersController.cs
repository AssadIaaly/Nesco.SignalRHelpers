using System.Security.Claims;
using ChatApplication.Server.Data;
using ChatApplication.Server.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nesco.SignalRUserManagement.Server.Services;

namespace ChatApplication.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISignalRUserManagementService _userManagementService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext dbContext,
        ISignalRUserManagementService userManagementService,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var users = await _dbContext.Users
            .Where(u => u.Id != currentUserId)
            .Select(u => new UserDto(
                u.Id,
                u.Email!,
                u.DisplayName,
                u.LastSeen,
                false, // Will be updated below
                u.ProfilePictureUrl
            ))
            .ToListAsync();

        // Update online status for each user
        var result = users.Select(u => u with
        {
            IsOnline = _userManagementService.IsUserConnected(u.Id)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(string userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var isOnline = _userManagementService.IsUserConnected(userId);

        return Ok(new UserDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.LastSeen,
            isOnline,
            user.ProfilePictureUrl
        ));
    }

    [HttpGet("{userId}/status")]
    public ActionResult<object> GetUserStatus(string userId)
    {
        var isOnline = _userManagementService.IsUserConnected(userId);
        return Ok(new { userId, isOnline });
    }

    [HttpGet("online")]
    public async Task<ActionResult<List<UserDto>>> GetOnlineUsers()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var connectedUsers = await _userManagementService.GetConnectedUsersAsync();

        var onlineUserIds = connectedUsers
            .Where(u => u.UserId != currentUserId)
            .Select(u => u.UserId)
            .Distinct()
            .ToList();

        var users = await _dbContext.Users
            .Where(u => onlineUserIds.Contains(u.Id))
            .Select(u => new UserDto(
                u.Id,
                u.Email!,
                u.DisplayName,
                u.LastSeen,
                true,
                u.ProfilePictureUrl
            ))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.LastSeen,
            true, // Current user is always online if making this request
            user.ProfilePictureUrl
        ));
    }

    [HttpPut("me/display-name")]
    public async Task<ActionResult> UpdateDisplayName([FromBody] UpdateDisplayNameRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.DisplayName = request.DisplayName;
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
}

public record UpdateDisplayNameRequest(string DisplayName);
