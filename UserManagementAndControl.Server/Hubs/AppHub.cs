using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Server.Hubs;
using Nesco.SignalRUserManagement.Server.Services;

namespace UserManagementAndControl.Server.Hubs;

/// <summary>
/// Custom hub that extends UserManagementHub with application-specific methods.
/// Clients can call these methods directly via the SignalR connection.
/// </summary>
public class AppHub : UserManagementHub
{
    private readonly ILogger<AppHub> _appLogger;

    public AppHub(
        InMemoryConnectionTracker tracker,
        ILogger<UserManagementHub> logger,
        ILogger<AppHub> appLogger,
        IResponseManager? responseManager = null)
        : base(tracker, logger, responseManager)
    {
        _appLogger = appLogger;
    }

    /// <summary>
    /// Returns the current server time in ISO 8601 format.
    /// Clients can call this to check connectivity and get server timestamp.
    /// </summary>
    public Task<string> GetServerTime()
    {
        _appLogger.LogInformation("GetServerTime called by {ConnectionId}", Context.ConnectionId);
        return Task.FromResult(DateTime.UtcNow.ToString("O"));
    }

    /// <summary>
    /// Echoes a message back to the caller.
    /// Useful for testing bidirectional communication.
    /// </summary>
    public Task<string> Echo(string message)
    {
        _appLogger.LogInformation("Echo called by {ConnectionId} with message: {Message}", Context.ConnectionId, message);
        return Task.FromResult($"Server received: {message}");
    }

    /// <summary>
    /// Sends a notification to a specific user (all their connections).
    /// </summary>
    public async Task SendNotificationToUser(string userId, string title, string message)
    {
        _appLogger.LogInformation("SendNotificationToUser called: UserId={UserId}, Title={Title}", userId, title);
        await Clients.User(userId).SendAsync("ReceiveNotification", new { Title = title, Message = message, Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Broadcasts a message to all connected clients.
    /// </summary>
    public async Task BroadcastMessage(string message)
    {
        _appLogger.LogInformation("BroadcastMessage called by {ConnectionId}: {Message}", Context.ConnectionId, message);
        await Clients.All.SendAsync("ReceiveBroadcast", new { Message = message, From = Context.UserIdentifier, Timestamp = DateTime.UtcNow });
    }
}
