using System.Collections.Concurrent;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// In-memory tracker for SignalR connections. Thread-safe and singleton.
/// No database dependency - connections are naturally cleaned up when the server restarts.
/// </summary>
public class InMemoryConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();

    /// <summary>
    /// Adds a connection for a user
    /// </summary>
    public void AddConnection(string connectionId, string userId, string? username = null)
    {
        _connections[connectionId] = new ConnectionInfo
        {
            ConnectionId = connectionId,
            UserId = userId,
            Username = username,
            ConnectedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Removes a connection
    /// </summary>
    public bool RemoveConnection(string connectionId)
    {
        return _connections.TryRemove(connectionId, out _);
    }

    /// <summary>
    /// Gets all connection IDs for a user
    /// </summary>
    public IReadOnlyList<string> GetUserConnections(string userId)
    {
        return _connections.Values
            .Where(c => c.UserId == userId)
            .Select(c => c.ConnectionId)
            .ToList();
    }

    /// <summary>
    /// Checks if a user has any connections
    /// </summary>
    public bool IsUserConnected(string userId)
    {
        return _connections.Values.Any(c => c.UserId == userId);
    }

    /// <summary>
    /// Gets the count of unique connected users
    /// </summary>
    public int GetConnectedUsersCount()
    {
        return _connections.Values
            .Select(c => c.UserId)
            .Distinct()
            .Count();
    }

    /// <summary>
    /// Gets all connections grouped by user
    /// </summary>
    public IReadOnlyList<UserConnectionGroup> GetAllConnections()
    {
        return _connections.Values
            .GroupBy(c => c.UserId)
            .Select(g => new UserConnectionGroup
            {
                UserId = g.Key,
                Username = g.First().Username,
                Connections = g.Select(c => new ConnectionInfo
                {
                    ConnectionId = c.ConnectionId,
                    UserId = c.UserId,
                    Username = c.Username,
                    ConnectedAt = c.ConnectedAt
                }).ToList()
            })
            .ToList();
    }

    /// <summary>
    /// Checks if a specific connection exists
    /// </summary>
    public bool ConnectionExists(string connectionId)
    {
        return _connections.ContainsKey(connectionId);
    }

    /// <summary>
    /// Gets all connection IDs
    /// </summary>
    public IReadOnlyList<string> GetAllConnectionIds()
    {
        return _connections.Keys.ToList();
    }

    /// <summary>
    /// Gets the total connection count
    /// </summary>
    public int GetConnectionCount()
    {
        return _connections.Count;
    }
}

/// <summary>
/// Information about a single connection
/// </summary>
public class ConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public DateTime ConnectedAt { get; set; }
}

/// <summary>
/// Grouped connections by user
/// </summary>
public class UserConnectionGroup
{
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public List<ConnectionInfo> Connections { get; set; } = new();
}
