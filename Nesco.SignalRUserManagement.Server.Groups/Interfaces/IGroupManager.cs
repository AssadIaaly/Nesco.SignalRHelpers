namespace Nesco.SignalRUserManagement.Server.Groups.Interfaces;

/// <summary>
/// Interface for managing SignalR group memberships.
/// Provides tracking of which users/connections belong to which groups.
/// </summary>
public interface IGroupManager
{
    /// <summary>
    /// Registers that a user/connection has joined a group.
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="groupName">The group name</param>
    /// <param name="connectionId">The SignalR connection ID</param>
    void RegisterGroupMembership(string userId, string groupName, string connectionId);

    /// <summary>
    /// Unregisters a user/connection from a specific group.
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="groupName">The group name</param>
    /// <param name="connectionId">The SignalR connection ID</param>
    void UnregisterGroupMembership(string userId, string groupName, string connectionId);

    /// <summary>
    /// Removes all group memberships for a specific connection (called on disconnect).
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID</param>
    void CleanupConnectionGroups(string connectionId);

    /// <summary>
    /// Gets all groups that a user belongs to.
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <returns>Collection of group names</returns>
    IEnumerable<string> GetUserGroups(string userId);

    /// <summary>
    /// Gets all connection IDs that belong to a specific group.
    /// </summary>
    /// <param name="groupName">The group name</param>
    /// <returns>Collection of connection IDs</returns>
    IEnumerable<string> GetGroupConnections(string groupName);

    /// <summary>
    /// Gets all user IDs that belong to a specific group.
    /// </summary>
    /// <param name="groupName">The group name</param>
    /// <returns>Collection of user IDs</returns>
    IEnumerable<string> GetGroupMembers(string groupName);
}
