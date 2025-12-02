using Microsoft.AspNetCore.SignalR.Client;

namespace UserManagementAndControl.Client.Extensions;

/// <summary>
/// Extension methods for calling custom hub methods on the server.
/// These provide type-safe wrappers around the hub method calls.
/// </summary>
public static class HubConnectionExtensions
{
    /// <summary>
    /// Gets the current server time in ISO 8601 format.
    /// </summary>
    public static Task<string> GetServerTimeAsync(this HubConnection connection)
    {
        return connection.InvokeAsync<string>("GetServerTime");
    }

    /// <summary>
    /// Echoes a message back from the server.
    /// </summary>
    public static Task<string> EchoAsync(this HubConnection connection, string message)
    {
        return connection.InvokeAsync<string>("Echo", message);
    }

    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    public static Task SendNotificationToUserAsync(this HubConnection connection, string userId, string title, string message)
    {
        return connection.InvokeAsync("SendNotificationToUser", userId, title, message);
    }

    /// <summary>
    /// Broadcasts a message to all connected clients.
    /// </summary>
    public static Task BroadcastMessageAsync(this HubConnection connection, string message)
    {
        return connection.InvokeAsync("BroadcastMessage", message);
    }
}
