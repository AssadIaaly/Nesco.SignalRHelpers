using Nesco.SignalRUserManagement.Client.Services;

namespace Nesco.SignalRUserManagement.Client.Groups.Extensions;

/// <summary>
/// Extension methods for adding group management functionality to UserConnectionClient.
/// </summary>
public static class UserConnectionClientGroupExtensions
{
    /// <summary>
    /// Joins a SignalR group on the server.
    /// The server must support group management (e.g., UserManagementHubWithGroups).
    /// </summary>
    /// <param name="client">The UserConnectionClient instance</param>
    /// <param name="groupName">The name of the group to join</param>
    /// <returns>Task representing the async operation</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected</exception>
    /// <example>
    /// <code>
    /// // Join a user-specific group
    /// await _client.JoinGroupAsync($"user_{userId}");
    ///
    /// // Join a profile-specific group
    /// await _client.JoinGroupAsync($"profile_{profileId}");
    /// </code>
    /// </example>
    public static async Task JoinGroupAsync(
        this UserConnectionClient client,
        string groupName)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));

        await client.SendAsync("JoinGroupAsync", groupName);
    }

    /// <summary>
    /// Leaves a SignalR group on the server.
    /// The server must support group management (e.g., UserManagementHubWithGroups).
    /// </summary>
    /// <param name="client">The UserConnectionClient instance</param>
    /// <param name="groupName">The name of the group to leave</param>
    /// <returns>Task representing the async operation</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected</exception>
    /// <example>
    /// <code>
    /// // Leave a profile group
    /// await _client.LeaveGroupAsync($"profile_{oldProfileId}");
    /// </code>
    /// </example>
    public static async Task LeaveGroupAsync(
        this UserConnectionClient client,
        string groupName)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));

        await client.SendAsync("LeaveGroupAsync", groupName);
    }

    /// <summary>
    /// Gets all groups that the current user belongs to.
    /// The server must support group management (e.g., UserManagementHubWithGroups).
    /// </summary>
    /// <param name="client">The UserConnectionClient instance</param>
    /// <returns>Array of group names the user belongs to</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected</exception>
    /// <example>
    /// <code>
    /// var groups = await _client.GetMyGroupsAsync();
    /// foreach (var group in groups)
    /// {
    ///     Console.WriteLine($"Member of group: {group}");
    /// }
    /// </code>
    /// </example>
    public static async Task<string[]> GetMyGroupsAsync(this UserConnectionClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        return await client.InvokeAsync<string[]>("GetMyGroups");
    }

    /// <summary>
    /// Gets all connection IDs in a specific group.
    /// The server must support group management (e.g., UserManagementHubWithGroups).
    /// May require admin privileges depending on server configuration.
    /// </summary>
    /// <param name="client">The UserConnectionClient instance</param>
    /// <param name="groupName">The name of the group</param>
    /// <returns>Array of connection IDs in the group</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected</exception>
    public static async Task<string[]> GetGroupConnectionsAsync(
        this UserConnectionClient client,
        string groupName)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));

        return await client.InvokeAsync<string[]>("GetGroupConnections", groupName);
    }

    /// <summary>
    /// Gets all user IDs in a specific group.
    /// The server must support group management (e.g., UserManagementHubWithGroups).
    /// May require admin privileges depending on server configuration.
    /// </summary>
    /// <param name="client">The UserConnectionClient instance</param>
    /// <param name="groupName">The name of the group</param>
    /// <returns>Array of user IDs in the group</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected</exception>
    public static async Task<string[]> GetGroupMembersAsync(
        this UserConnectionClient client,
        string groupName)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));

        return await client.InvokeAsync<string[]>("GetGroupMembers", groupName);
    }
}
