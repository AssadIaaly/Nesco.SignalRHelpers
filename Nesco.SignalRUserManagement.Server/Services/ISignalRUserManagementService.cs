using Nesco.SignalRUserManagement.Core.Models;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Unified service interface for SignalR User Management with method invocation support.
/// Provides a single interface for managing connections and invoking methods on clients.
/// </summary>
public interface ISignalRUserManagementService
{
    /// <summary>
    /// Invokes a method on all connected users
    /// </summary>
    Task<SignalRResponse> InvokeOnAllConnectedAsync(string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on a specific user (all their connections)
    /// </summary>
    Task<SignalRResponse> InvokeOnUserAsync(string userId, string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on multiple users (all their connections)
    /// </summary>
    Task<SignalRResponse> InvokeOnUsersAsync(IEnumerable<string> userIds, string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on a specific connection
    /// </summary>
    Task<SignalRResponse> InvokeOnConnectionAsync(string connectionId, string methodName, object? parameter);

    // ===== Streaming Multi-Response Methods =====

    /// <summary>
    /// Invokes a method on all connected users and streams responses as they arrive from each connection.
    /// </summary>
    /// <param name="methodName">Method name to invoke</param>
    /// <param name="parameter">Parameter to pass</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable that yields responses as they arrive</returns>
    IAsyncEnumerable<ClientResponse> InvokeOnAllConnectedStreamingAsync(string methodName, object? parameter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a method on a specific user (all their connections) and streams responses as they arrive.
    /// </summary>
    IAsyncEnumerable<ClientResponse> InvokeOnUserStreamingAsync(string userId, string methodName, object? parameter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a method on multiple users (all their connections) and streams responses as they arrive.
    /// </summary>
    IAsyncEnumerable<ClientResponse> InvokeOnUsersStreamingAsync(IEnumerable<string> userIds, string methodName, object? parameter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a method on multiple connections and streams responses as they arrive.
    /// </summary>
    IAsyncEnumerable<ClientResponse> InvokeOnConnectionsStreamingAsync(IEnumerable<string> connectionIds, string methodName, object? parameter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of connected users
    /// </summary>
    int GetConnectedUsersCount();

    /// <summary>
    /// Checks if a user is connected
    /// </summary>
    bool IsUserConnected(string userId);

    /// <summary>
    /// Gets all connected users with their connections
    /// </summary>
    Task<List<ConnectedUserInfo>> GetConnectedUsersAsync();
}

/// <summary>
/// DTO for connected user information
/// </summary>
public class ConnectedUserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public DateTime? LastConnect { get; set; }
    public List<UserConnectionInfo> Connections { get; set; } = new();
}

/// <summary>
/// DTO for connection information
/// </summary>
public class UserConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ConnectedAt { get; set; }
}
