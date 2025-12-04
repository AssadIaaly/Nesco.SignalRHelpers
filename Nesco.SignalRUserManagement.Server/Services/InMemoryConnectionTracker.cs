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
    /// Event raised when a connection is added or removed
    /// </summary>
    public event Action<ConnectionChangeEventArgs>? ConnectionsChanged;

    /// <summary>
    /// Adds a connection for a user
    /// </summary>
    public void AddConnection(string connectionId, string userId, string? username = null)
    {
        var info = new ConnectionInfo
        {
            ConnectionId = connectionId,
            UserId = userId,
            Username = username,
            ConnectedAt = DateTime.UtcNow
        };
        _connections[connectionId] = info;

        ConnectionsChanged?.Invoke(new ConnectionChangeEventArgs
        {
            ChangeType = ConnectionChangeType.Connected,
            ConnectionId = connectionId,
            UserId = userId,
            Username = username
        });
    }

    /// <summary>
    /// Removes a connection
    /// </summary>
    public bool RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var info))
        {
            ConnectionsChanged?.Invoke(new ConnectionChangeEventArgs
            {
                ChangeType = ConnectionChangeType.Disconnected,
                ConnectionId = connectionId,
                UserId = info.UserId,
                Username = info.Username
            });
            return true;
        }
        return false;
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

/// <summary>
/// Event args for connection changes
/// </summary>
public class ConnectionChangeEventArgs
{
    public ConnectionChangeType ChangeType { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
}

/// <summary>
/// Type of connection change
/// </summary>
public enum ConnectionChangeType
{
    Connected,
    Disconnected
}
