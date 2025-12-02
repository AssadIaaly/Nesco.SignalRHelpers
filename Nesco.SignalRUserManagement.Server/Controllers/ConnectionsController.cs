using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Server.Models;

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

    public ConnectionsController(
        IUserConnectionDbContext dbContext,
        IUserConnectionService connectionService)
    {
        _dbContext = dbContext;
        _connectionService = connectionService;
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
                // Get username from the first connection (all connections for a user should have the same username)
                UserName = g.First().Username,
                Connections = g.Select(c => new ConnectionDto
                {
                    ConnectionId = c.ConnectionId,
                    ConnectedAt = c.ConnectedAt
                }).ToList()
            })
            .ToListAsync();

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
        var userConnections = await _dbContext.UserConnections
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!userConnections.Any())
        {
            return NotFound();
        }

        var result = new ConnectedUserDto
        {
            UserId = userId,
            // Get username from the first connection
            UserName = userConnections.First().Username,
            Connections = userConnections.Select(c => new ConnectionDto
            {
                ConnectionId = c.ConnectionId,
                ConnectedAt = c.ConnectedAt
            }).ToList()
        };

        return Ok(result);
    }
}
