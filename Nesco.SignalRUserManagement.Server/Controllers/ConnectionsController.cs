using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nesco.SignalRUserManagement.Server.Models;
using Nesco.SignalRUserManagement.Server.Services;

namespace Nesco.SignalRUserManagement.Server.Controllers;

/// <summary>
/// API controller for querying connected users.
/// Uses in-memory connection tracking.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly InMemoryConnectionTracker _tracker;

    public ConnectionsController(InMemoryConnectionTracker tracker)
    {
        _tracker = tracker;
    }

    /// <summary>
    /// Gets all connected users with their connections
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<ConnectedUserDto>> GetConnectedUsers()
    {
        var groups = _tracker.GetAllConnections();

        var result = groups.Select(g => new ConnectedUserDto
        {
            UserId = g.UserId,
            UserName = g.Username,
            Connections = g.Connections.Select(c => new ConnectionDto
            {
                ConnectionId = c.ConnectionId,
                ConnectedAt = c.ConnectedAt
            }).ToList()
        });

        return Ok(result);
    }

    /// <summary>
    /// Gets the total number of active connections
    /// </summary>
    [HttpGet("count")]
    public ActionResult<int> GetConnectionCount()
    {
        return Ok(_tracker.GetConnectionCount());
    }

    /// <summary>
    /// Gets the number of unique connected users
    /// </summary>
    [HttpGet("users/count")]
    public ActionResult<int> GetConnectedUsersCount()
    {
        return Ok(_tracker.GetConnectedUsersCount());
    }

    /// <summary>
    /// Checks if a specific user is connected
    /// </summary>
    [HttpGet("users/{userId}/status")]
    public ActionResult<bool> IsUserConnected(string userId)
    {
        return Ok(_tracker.IsUserConnected(userId));
    }

    /// <summary>
    /// Gets connections for a specific user
    /// </summary>
    [HttpGet("users/{userId}")]
    public ActionResult<ConnectedUserDto> GetUserConnections(string userId)
    {
        var connections = _tracker.GetUserConnections(userId);

        if (connections.Count == 0)
        {
            return NotFound();
        }

        var groups = _tracker.GetAllConnections();
        var userGroup = groups.FirstOrDefault(g => g.UserId == userId);

        if (userGroup == null)
        {
            return NotFound();
        }

        var result = new ConnectedUserDto
        {
            UserId = userId,
            UserName = userGroup.Username,
            Connections = userGroup.Connections.Select(c => new ConnectionDto
            {
                ConnectionId = c.ConnectionId,
                ConnectedAt = c.ConnectedAt
            }).ToList()
        };

        return Ok(result);
    }
}
