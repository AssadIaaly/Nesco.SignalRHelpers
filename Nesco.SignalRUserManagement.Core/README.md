# Nesco.SignalRUserManagement.Core

Core models and interfaces for SignalR User Management. This library provides the foundational types for tracking and managing SignalR connections across your application.

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Core
```

## Overview

This package contains:

- **Interfaces** - Contracts for user connection services
- **Models** - DTOs for representing users, connections, and events
- **Options** - Configuration options for customizing behavior

## Components

### IUserConnectionService

The main interface for managing SignalR connections:

```csharp
public interface IUserConnectionService
{
    // Send messages
    Task SendToAllAsync(string method, object? data = null);
    Task SendToUserAsync(string userId, string method, object? data = null);
    Task SendToConnectionAsync(string connectionId, string method, object? data = null);
    Task SendToUsersAsync(IEnumerable<string> userIds, string method, object? data = null);

    // Query connection state
    int GetConnectedUsersCount();
    int GetActiveConnectionsCount();
    bool IsUserConnected(string userId);
}
```

### Models

#### ConnectedUserDTO

Represents a connected user with their active connections:

```csharp
public class ConnectedUserDTO
{
    public string UserId { get; set; }
    public DateTime? LastConnect { get; set; }
    public DateTime? LastDisconnect { get; set; }
    public List<ConnectionDTO> Connections { get; set; }
    public int NumberOfConnections { get; }
}
```

#### ConnectionDTO

Represents a single SignalR connection:

```csharp
public class ConnectionDTO
{
    public string ConnectionId { get; set; }
    public string UserAgent { get; set; }
    public bool Connected { get; set; }
    public DateTime ConnectedAt { get; set; }
}
```

#### UserConnectionEventArgs

Event data for connection events:

```csharp
public class UserConnectionEventArgs
{
    public string UserId { get; set; }
    public string ConnectionId { get; set; }
    public string UserAgent { get; set; }
    public UserConnectionEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum UserConnectionEventType
{
    Connected,
    Disconnected,
    Reconnected
}
```

### UserManagementOptions

Configuration options for customizing behavior:

| Option | Default | Description |
|--------|---------|-------------|
| `BroadcastConnectionEvents` | `true` | Broadcast connection events to all clients |
| `ConnectionEventMethod` | `"UserConnectionEvent"` | Method name for connection event broadcasts |
| `AutoPurgeOfflineConnections` | `true` | Automatically purge offline connections on connect |
| `KeepAliveIntervalSeconds` | `15` | Keep-alive interval for SignalR connections |
| `ClientTimeoutSeconds` | `30` | Client timeout in seconds |
| `TrackUserAgent` | `true` | Track user agent information |
| `AutoReconnect` | `true` | Automatically reconnect when connection drops |
| `AutoReconnectRetryDelaysSeconds` | `[0, 2, 5, 10, 30]` | Progressive backoff delays for reconnection |
| `OnUserConnected` | `null` | Callback invoked after user connects |
| `OnUserDisconnected` | `null` | Callback invoked before user disconnects |

## Requirements

- .NET 10.0+

## License

See the LICENSE file for details.
