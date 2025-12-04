using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Service for sending messages to connected users.
/// Uses in-memory connection tracking - no database required.
/// </summary>
public class UserConnectionService<THub> : IUserConnectionService
    where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;
    private readonly InMemoryConnectionTracker _tracker;
    private readonly ILogger<UserConnectionService<THub>> _logger;

    public UserConnectionService(
        IHubContext<THub> hubContext,
        InMemoryConnectionTracker tracker,
        ILogger<UserConnectionService<THub>> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendToAllAsync(string method, object? data = null)
    {
        await _hubContext.Clients.All.SendAsync(method, data);
    }

    public async Task SendToUserAsync(string userId, string method, object? data = null)
    {
        // Use SignalR's native user targeting - more efficient
        await _hubContext.Clients.User(userId).SendAsync(method, data);
    }

    public async Task SendToConnectionAsync(string connectionId, string method, object? data = null)
    {
        await _hubContext.Clients.Client(connectionId).SendAsync(method, data);
    }

    public bool IsUserConnected(string userId)
    {
        return _tracker.IsUserConnected(userId);
    }

    public int GetConnectedUsersCount()
    {
        return _tracker.GetConnectedUsersCount();
    }

    public Task<IReadOnlyList<string>> GetUserConnectionsAsync(string userId)
    {
        return Task.FromResult(_tracker.GetUserConnections(userId));
    }

    /// <summary>
    /// No-op for in-memory tracking. Connections are automatically cleaned up
    /// when OnDisconnectedAsync is called by SignalR.
    /// </summary>
    public Task<int> PurgeStaleConnectionsAsync()
    {
        // With in-memory tracking, there's nothing to purge.
        // SignalR's OnDisconnectedAsync handles cleanup automatically.
        _logger.LogDebug("PurgeStaleConnectionsAsync called - no-op with in-memory tracking");
        return Task.FromResult(0);
    }
}
