using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Server.Models;
using Nesco.SignalRUserManagement.Server.Services;

namespace Nesco.SignalRUserManagement.Server.Controllers;

/// <summary>
/// API controller for querying connected users.
/// Uses IUserConnectionDbContext to access the UserConnections DbSet.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly IUserConnectionDbContext _dbContext;
    private readonly IUserConnectionService _connectionService;
    private readonly IUserNameResolver? _userNameResolver;

    public ConnectionsController(
        IUserConnectionDbContext dbContext,
        IUserConnectionService connectionService,
        IUserNameResolver? userNameResolver = null)
    {
        _dbContext = dbContext;
        _connectionService = connectionService;
        _userNameResolver = userNameResolver;
    }

    /// <summary>
    /// Gets all connected users with their connections
    /// </summary>
    /// <param name="purgeStale">Whether to purge stale connections first (default: true)</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConnectedUserDto>>> GetConnectedUsers(
        [FromQuery] bool purgeStale = true)
    {
        if (purgeStale)
        {
            await _connectionService.PurgeStaleConnectionsAsync();
        }

        var connections = await _dbContext.UserConnections
            .GroupBy(c => c.UserId)
            .Select(g => new ConnectedUserDto
            {
                UserId = g.Key,
                Connections = g.Select(c => new ConnectionDto
                {
                    ConnectionId = c.ConnectionId,
                    ConnectedAt = c.ConnectedAt
                }).ToList()
            })
            .ToListAsync();

        // Resolve user names if resolver is available
        if (_userNameResolver != null && connections.Count > 0)
        {
            var userIds = connections.Select(c => c.UserId).ToList();
            var userNames = await _userNameResolver.ResolveUserNamesAsync(userIds);
            foreach (var connection in connections)
            {
                if (userNames.TryGetValue(connection.UserId, out var userName))
                {
                    connection.UserName = userName;
                }
            }
        }

        return Ok(connections);
    }

    /// <summary>
    /// Purges stale connections from the database
    /// </summary>
    /// <returns>Number of connections purged</returns>
    [HttpPost("purge")]
    public async Task<ActionResult<int>> PurgeStaleConnections()
    {
        var count = await _connectionService.PurgeStaleConnectionsAsync();
        return Ok(count);
    }

    /// <summary>
    /// Gets the total number of active connections
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetConnectionCount()
    {
        var count = await _dbContext.UserConnections.CountAsync();
        return Ok(count);
    }

    /// <summary>
    /// Gets the number of unique connected users
    /// </summary>
    [HttpGet("users/count")]
    public async Task<ActionResult<int>> GetConnectedUsersCount()
    {
        var count = await _dbContext.UserConnections
            .Select(c => c.UserId)
            .Distinct()
            .CountAsync();
        return Ok(count);
    }

    /// <summary>
    /// Checks if a specific user is connected
    /// </summary>
    [HttpGet("users/{userId}/status")]
    public async Task<ActionResult<bool>> IsUserConnected(string userId)
    {
        var isConnected = await _dbContext.UserConnections
            .AnyAsync(c => c.UserId == userId);
        return Ok(isConnected);
    }

    /// <summary>
    /// Gets connections for a specific user
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<ConnectedUserDto>> GetUserConnections(string userId)
    {
        var connections = await _dbContext.UserConnections
            .Where(c => c.UserId == userId)
            .Select(c => new ConnectionDto
            {
                ConnectionId = c.ConnectionId,
                ConnectedAt = c.ConnectedAt
            })
            .ToListAsync();

        if (!connections.Any())
        {
            return NotFound();
        }

        var result = new ConnectedUserDto
        {
            UserId = userId,
            Connections = connections
        };

        // Resolve user name if resolver is available
        if (_userNameResolver != null)
        {
            var userNames = await _userNameResolver.ResolveUserNamesAsync([userId]);
            if (userNames.TryGetValue(userId, out var userName))
            {
                result.UserName = userName;
            }
        }

        return Ok(result);
    }
}
