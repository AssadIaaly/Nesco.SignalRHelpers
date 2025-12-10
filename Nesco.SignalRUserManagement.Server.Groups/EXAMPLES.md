# Usage Examples

## Example 1: Basic Group Management

```csharp
// Server
public class MyHub : UserManagementHubWithGroups
{
    public MyHub(/* dependencies */) : base(/* pass to base */) { }
}

// Client
await client.JoinGroupAsync("notifications");
```

## Example 2: Prayer Times Application Pattern

If you're migrating from a custom hub with specific method names, you can create an adapter hub in your application:

```csharp
// In your PrayerTimesServer project (NOT in the library)
public class NotifyHub : UserManagementHubWithGroups
{
    private readonly WallpaperRotationService _wallpaperService;
    private readonly IConnectionTrackingService _connectionTracking;

    public NotifyHub(
        InMemoryConnectionTracker tracker,
        ILogger<UserManagementHub> logger,
        WallpaperRotationService wallpaperService,
        IConnectionTrackingService connectionTracking,
        IResponseManager? responseManager = null,
        IGroupManager? groupManager = null)
        : base(tracker, logger, responseManager, groupManager)
    {
        _wallpaperService = wallpaperService;
        _connectionTracking = connectionTracking;
    }

    // Adapter methods for backward compatibility with existing clients
    public async Task JoinUserGroup(string userId, string? sessionId = null)
    {
        await JoinGroupAsync($"user_{userId}");
        await _connectionTracking.RegisterConnectionAsync(userId, Context.ConnectionId);
    }

    public async Task JoinProfileGroup(Guid profileId)
    {
        await JoinGroupAsync($"profile_{profileId}");
    }

    public async Task LeaveProfileGroup(Guid profileId)
    {
        await LeaveGroupAsync($"profile_{profileId}");
    }

    // Your custom domain methods
    public async Task RequestCurrentWallpaperIndex(Guid profileId)
    {
        var index = _wallpaperService.GetCurrentWallpaperIndex(profileId);
        await Clients.Caller.SendAsync("WallpaperIndexChanged", index);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _connectionTracking.UnregisterConnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
```

## Example 3: Multi-Tenant Application

```csharp
public class TenantHub : UserManagementHubWithGroups
{
    public override async Task JoinGroupAsync(string groupName)
    {
        // Get tenant from claims
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantId))
            throw new UnauthorizedAccessException("No tenant ID found");

        // Prefix group name with tenant to ensure isolation
        var tenantGroupName = $"tenant_{tenantId}_{groupName}";
        await base.JoinGroupAsync(tenantGroupName);
    }
}
```

## Example 4: Role-Based Group Access

```csharp
public class SecureHub : UserManagementHubWithGroups
{
    public override async Task JoinGroupAsync(string groupName)
    {
        // Only allow users to join groups they're authorized for
        if (groupName.StartsWith("admin_") && !Context.User.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Admin role required");

        await base.JoinGroupAsync(groupName);
    }
}
```

## Example 5: Using IGroupManager in Services

```csharp
public class NotificationService
{
    private readonly IHubContext<UserManagementHubWithGroups> _hub;
    private readonly IGroupManager _groupManager;

    public NotificationService(
        IHubContext<UserManagementHubWithGroups> hub,
        IGroupManager groupManager)
    {
        _hub = hub;
        _groupManager = groupManager;
    }

    public async Task NotifyAllUsersInGroup(string groupName, string message)
    {
        // Get all users in the group
        var members = _groupManager.GetGroupMembers(groupName);

        Console.WriteLine($"Notifying {members.Count()} users in group {groupName}");

        // Send message to the group
        await _hub.Clients.Group(groupName).SendAsync("Notification", message);
    }

    public async Task NotifyUser(string userId, string message)
    {
        // Send to user's personal group
        await _hub.Clients.Group($"user_{userId}").SendAsync("Notification", message);
    }
}
```
