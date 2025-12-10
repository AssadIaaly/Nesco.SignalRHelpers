using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Server.Groups.Interfaces;
using Nesco.SignalRUserManagement.Server.Hubs;
using Nesco.SignalRUserManagement.Server.Services;
using IGroupManager = Nesco.SignalRUserManagement.Server.Groups.Interfaces.IGroupManager;

namespace Nesco.SignalRUserManagement.Server.Groups.Hubs;

/// <summary>
/// Extended UserManagementHub that adds group management capabilities.
/// Backward compatible - works as UserManagementHub if IGroupManager is not registered.
/// </summary>
public class UserManagementHubWithGroups : UserManagementHub
{
    private readonly IGroupManager? _groupManager;

    public UserManagementHubWithGroups(
        InMemoryConnectionTracker tracker,
        ILogger<UserManagementHub> logger,
        IResponseManager? responseManager = null,
        IGroupManager? groupManager = null)
        : base(tracker, logger, responseManager)
    {
        _groupManager = groupManager;
    }

    /// <summary>
    /// Allows clients to join a custom group.
    /// Virtual so it can be overridden for additional validation (e.g., session validation).
    /// </summary>
    /// <param name="groupName">The name of the group to join</param>
    public virtual async Task JoinGroupAsync(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));
        }

        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Cannot join group: User identifier is not available");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _groupManager?.RegisterGroupMembership(userId, groupName, Context.ConnectionId);

        Console.WriteLine($"[SIGNALR-GROUPS] User {userId} (Connection: {Context.ConnectionId}) joined group {groupName}");
    }

    /// <summary>
    /// Allows clients to leave a custom group.
    /// </summary>
    /// <param name="groupName">The name of the group to leave</param>
    public virtual async Task LeaveGroupAsync(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));
        }

        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Cannot leave group: User identifier is not available");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _groupManager?.UnregisterGroupMembership(userId, groupName, Context.ConnectionId);

        Console.WriteLine($"[SIGNALR-GROUPS] User {userId} (Connection: {Context.ConnectionId}) left group {groupName}");
    }

    /// <summary>
    /// Override to clean up group memberships when a connection disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _groupManager?.CleanupConnectionGroups(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets all groups that the calling user belongs to.
    /// Useful for clients to know their current group memberships.
    /// </summary>
    /// <returns>Array of group names</returns>
    public string[] GetMyGroups()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<string>();
        }

        return _groupManager?.GetUserGroups(userId).ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets all connection IDs in a specific group.
    /// Useful for server-side code to know who is in a group.
    /// Can be restricted to admin users if needed.
    /// </summary>
    /// <param name="groupName">The group name</param>
    /// <returns>Array of connection IDs</returns>
    public string[] GetGroupConnections(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return Array.Empty<string>();
        }

        return _groupManager?.GetGroupConnections(groupName).ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets all user IDs in a specific group.
    /// Useful for server-side code to know who is in a group.
    /// Can be restricted to admin users if needed.
    /// </summary>
    /// <param name="groupName">The group name</param>
    /// <returns>Array of user IDs</returns>
    public string[] GetGroupMembers(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return Array.Empty<string>();
        }

        return _groupManager?.GetGroupMembers(groupName).ToArray() ?? Array.Empty<string>();
    }
}
