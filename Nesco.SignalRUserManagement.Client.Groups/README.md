# Nesco.SignalRUserManagement.Client.Groups

Client-side extension methods for group management with Nesco.SignalRUserManagement.Client.

## Features

- ✅ Join/leave groups via extension methods
- ✅ Query group membership
- ✅ Works with UserConnectionClient
- ✅ Type-safe API

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Client.Groups
```

## Usage

### 1. Add Using Statement

```csharp
using Nesco.SignalRUserManagement.Client.Groups.Extensions;
```

### 2. Use Extension Methods

```csharp
// Inject UserConnectionClient
private readonly UserConnectionClient _client;

// Join groups
await _client.JoinGroupAsync("user_123");
await _client.JoinGroupAsync($"profile_{profileId}");

// Leave groups
await _client.LeaveGroupAsync($"profile_{oldProfileId}");

// Get my groups
var groups = await _client.GetMyGroupsAsync();
foreach (var group in groups)
{
    Console.WriteLine($"Member of: {group}");
}
```

### 3. Example: Profile Switching

```csharp
public async Task SwitchProfile(Guid oldProfileId, Guid newProfileId)
{
    // Leave old profile group
    await _client.LeaveGroupAsync($"profile_{oldProfileId}");

    // Join new profile group
    await _client.JoinGroupAsync($"profile_{newProfileId}");
}
```

### 4. Example: User Groups

```csharp
public async Task ConnectAndJoinUserGroup(string userId)
{
    // Start connection
    await _client.StartAsync();

    // Join user-specific group for notifications
    await _client.JoinGroupAsync($"user_{userId}");
}
```

## API Reference

### Extension Methods

All methods extend `UserConnectionClient`:

- `Task JoinGroupAsync(string groupName)` - Join a group
- `Task LeaveGroupAsync(string groupName)` - Leave a group
- `Task<string[]> GetMyGroupsAsync()` - Get all groups you belong to
- `Task<string[]> GetGroupConnectionsAsync(string groupName)` - Get connections in a group (may require admin)
- `Task<string[]> GetGroupMembersAsync(string groupName)` - Get user IDs in a group (may require admin)

## Server Requirements

The server must use `UserManagementHubWithGroups` or a derived hub that supports group management.

```csharp
// Server-side
app.MapHub<UserManagementHubWithGroups>("/hubs/usermanagement");
```

## Error Handling

```csharp
try
{
    await _client.JoinGroupAsync("mygroup");
}
catch (InvalidOperationException ex)
{
    // Not connected
    Console.WriteLine($"Cannot join group: {ex.Message}");
}
catch (Exception ex)
{
    // Other errors (network, authorization, etc.)
    Console.WriteLine($"Error joining group: {ex.Message}");
}
```
