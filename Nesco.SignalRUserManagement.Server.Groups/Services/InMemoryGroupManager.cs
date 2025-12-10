using System.Collections.Concurrent;
using Nesco.SignalRUserManagement.Server.Groups.Interfaces;

namespace Nesco.SignalRUserManagement.Server.Groups.Services;

/// <summary>
/// In-memory implementation of IGroupManager.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public class InMemoryGroupManager : IGroupManager
{
    // Track which groups each user belongs to: userId -> set of group names
    private readonly ConcurrentDictionary<string, HashSet<string>> _userGroups = new();

    // Track which connections belong to each group: groupName -> set of connection IDs
    private readonly ConcurrentDictionary<string, HashSet<string>> _groupConnections = new();

    // Track which groups each connection belongs to: connectionId -> set of group names
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectionGroups = new();

    // Track which user each connection belongs to: connectionId -> userId
    private readonly ConcurrentDictionary<string, string> _connectionUsers = new();

    // Lock objects for thread-safe HashSet operations
    private readonly object _lock = new();

    public void RegisterGroupMembership(string userId, string groupName, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("GroupName cannot be null or empty", nameof(groupName));
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId cannot be null or empty", nameof(connectionId));

        lock (_lock)
        {
            // Add group to user's group list
            _userGroups.AddOrUpdate(
                userId,
                _ => new HashSet<string> { groupName },
                (_, groups) => { groups.Add(groupName); return groups; });

            // Add connection to group's connection list
            _groupConnections.AddOrUpdate(
                groupName,
                _ => new HashSet<string> { connectionId },
                (_, connections) => { connections.Add(connectionId); return connections; });

            // Add group to connection's group list
            _connectionGroups.AddOrUpdate(
                connectionId,
                _ => new HashSet<string> { groupName },
                (_, groups) => { groups.Add(groupName); return groups; });

            // Map connection to user
            _connectionUsers[connectionId] = userId;
        }
    }

    public void UnregisterGroupMembership(string userId, string groupName, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("GroupName cannot be null or empty", nameof(groupName));
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId cannot be null or empty", nameof(connectionId));

        lock (_lock)
        {
            // Remove group from user's group list
            if (_userGroups.TryGetValue(userId, out var userGroups))
            {
                userGroups.Remove(groupName);
                if (userGroups.Count == 0)
                    _userGroups.TryRemove(userId, out _);
            }

            // Remove connection from group's connection list
            if (_groupConnections.TryGetValue(groupName, out var groupConns))
            {
                groupConns.Remove(connectionId);
                if (groupConns.Count == 0)
                    _groupConnections.TryRemove(groupName, out _);
            }

            // Remove group from connection's group list
            if (_connectionGroups.TryGetValue(connectionId, out var connGroups))
            {
                connGroups.Remove(groupName);
                if (connGroups.Count == 0)
                    _connectionGroups.TryRemove(connectionId, out _);
            }
        }
    }

    public void CleanupConnectionGroups(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId cannot be null or empty", nameof(connectionId));

        lock (_lock)
        {
            // Get all groups this connection belongs to
            if (!_connectionGroups.TryRemove(connectionId, out var groups))
                return;

            // Get the userId for this connection
            _connectionUsers.TryRemove(connectionId, out var userId);

            // Remove this connection from all its groups
            foreach (var groupName in groups)
            {
                if (_groupConnections.TryGetValue(groupName, out var groupConns))
                {
                    groupConns.Remove(connectionId);
                    if (groupConns.Count == 0)
                        _groupConnections.TryRemove(groupName, out _);
                }

                // If we know the userId, remove the group from user's list if no more connections
                if (!string.IsNullOrEmpty(userId) &&
                    _userGroups.TryGetValue(userId, out var userGroups))
                {
                    // Check if user has any other connections in this group
                    var hasOtherConnections = _groupConnections.TryGetValue(groupName, out var conns) &&
                                              conns.Any(c => _connectionUsers.TryGetValue(c, out var u) && u == userId);

                    if (!hasOtherConnections)
                    {
                        userGroups.Remove(groupName);
                        if (userGroups.Count == 0)
                            _userGroups.TryRemove(userId, out _);
                    }
                }
            }
        }
    }

    public IEnumerable<string> GetUserGroups(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Enumerable.Empty<string>();

        lock (_lock)
        {
            return _userGroups.TryGetValue(userId, out var groups)
                ? groups.ToArray()
                : Enumerable.Empty<string>();
        }
    }

    public IEnumerable<string> GetGroupConnections(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return Enumerable.Empty<string>();

        lock (_lock)
        {
            return _groupConnections.TryGetValue(groupName, out var connections)
                ? connections.ToArray()
                : Enumerable.Empty<string>();
        }
    }

    public IEnumerable<string> GetGroupMembers(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return Enumerable.Empty<string>();

        lock (_lock)
        {
            if (!_groupConnections.TryGetValue(groupName, out var connections))
                return Enumerable.Empty<string>();

            var userIds = new HashSet<string>();
            foreach (var connectionId in connections)
            {
                if (_connectionUsers.TryGetValue(connectionId, out var userId))
                    userIds.Add(userId);
            }

            return userIds.ToArray();
        }
    }
}
