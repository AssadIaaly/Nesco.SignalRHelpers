# Nesco.SignalRUserManagement.Server.Groups

Adds group management capabilities to Nesco.SignalRUserManagement.Server.

## Features

- ✅ Group join/leave operations
- ✅ Automatic group cleanup on disconnect
- ✅ In-memory group tracking
- ✅ Query group membership
- ✅ Backward compatible with existing UserManagementHub

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Server.Groups
```

## Usage

### 1. Register Services

```csharp
// In Program.cs
builder.Services.AddSignalRGroupManagement();
```

### 2. Use Extended Hub

```csharp
// Map the hub with group support
app.MapHub<UserManagementHubWithGroups>("/hubs/usermanagement");
```

### 3. Clients Can Join Groups

```csharp
// Client-side (JavaScript, C#, etc.)
await connection.invoke("JoinGroupAsync", "user_123");
await connection.invoke("JoinGroupAsync", "profile_456");
```

### 4. Server Can Send to Groups

```csharp
// Inject IHubContext
public class MyService
{
    private readonly IHubContext<UserManagementHubWithGroups> _hub;

    public async Task NotifyGroup(string groupName, string message)
    {
        await _hub.Clients.Group(groupName).SendAsync("Notification", message);
    }
}
```

## Extending for Custom Validation

You can inherit from `UserManagementHubWithGroups` to add custom validation:

```csharp
public class MyCustomHub : UserManagementHubWithGroups
{
    public MyCustomHub(/* dependencies */) : base(/* pass to base */) { }

    // Override JoinGroupAsync for custom validation
    public override async Task JoinGroupAsync(string groupName)
    {
        // Custom validation logic
        if (!IsAuthorized(groupName))
        {
            throw new UnauthorizedAccessException("Not authorized to join this group");
        }

        // Call base implementation
        await base.JoinGroupAsync(groupName);
    }
}
```

## API Reference

### Hub Methods

- `JoinGroupAsync(string groupName)` - Join a group
- `LeaveGroupAsync(string groupName)` - Leave a group
- `GetMyGroups()` - Get all groups current user belongs to
- `GetGroupConnections(string groupName)` - Get all connection IDs in a group
- `GetGroupMembers(string groupName)` - Get all user IDs in a group

### IGroupManager

Injected service for group tracking:

```csharp
public interface IGroupManager
{
    void RegisterGroupMembership(string userId, string groupName, string connectionId);
    void UnregisterGroupMembership(string userId, string groupName, string connectionId);
    void CleanupConnectionGroups(string connectionId);
    IEnumerable<string> GetUserGroups(string userId);
    IEnumerable<string> GetGroupConnections(string groupName);
    IEnumerable<string> GetGroupMembers(string groupName);
}
```

## Custom Group Manager

Provide your own implementation (e.g., database-backed):

```csharp
public class DbGroupManager : IGroupManager
{
    // Your implementation
}

// Register it
builder.Services.AddSignalRGroupManagement<DbGroupManager>();
```
