namespace Nesco.SignalRUserManagement.Core.Interfaces;

/// <summary>
/// Service for querying and sending messages to connected users
/// </summary>
public interface IUserConnectionService
{
    /// <summary>
    /// Sends a message to all connected clients
    /// </summary>
    Task SendToAllAsync(string method, object? data = null);

    /// <summary>
    /// Sends a message to a specific user's connections
    /// </summary>
    Task SendToUserAsync(string userId, string method, object? data = null);

    /// <summary>
    /// Sends a message to a specific connection
    /// </summary>
    Task SendToConnectionAsync(string connectionId, string method, object? data = null);

    /// <summary>
    /// Checks if a user has any active connections
    /// </summary>
    bool IsUserConnected(string userId);

    /// <summary>
    /// Gets the count of connected users
    /// </summary>
    int GetConnectedUsersCount();

    /// <summary>
    /// Gets all connection IDs for a user
    /// </summary>
    Task<IReadOnlyList<string>> GetUserConnectionsAsync(string userId);

    /// <summary>
    /// Removes stale connections from the database that are no longer active in the hub.
    /// Returns the number of connections removed.
    /// </summary>
    Task<int> PurgeStaleConnectionsAsync();
}
