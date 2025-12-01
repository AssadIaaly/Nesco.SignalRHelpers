# Nesco.SignalRCommunicator.Client.Dashboard

A ready-to-use Blazor WebAssembly dashboard component for SignalR client status, connection monitoring, and method invocation logging.

## Installation

```bash
dotnet add package Nesco.SignalRCommunicator.Client.Dashboard
```

## Features

- Real-time connection status monitoring for User Management and Communicator hubs
- Display of registered client methods that can be invoked by the server
- Method invocation logging with parameters, results, duration, and status
- Auto-reconnect status indicators
- Clear, responsive UI with Bootstrap styling

## Quick Start

### 1. Register Services

In your `Program.cs`:

```csharp
using Nesco.SignalRCommunicator.Client.Dashboard;

builder.Services.AddSignalRClientDashboard();
```

Or with pre-registered methods:

```csharp
builder.Services.AddSignalRClientDashboard(registry =>
{
    registry.RegisterMethod("Ping", "", "string", "Default connectivity check", isDefault: true);
    registry.RegisterMethod("GetClientInfo", "", "ClientInfo", "Returns client information");
    registry.RegisterMethod("Calculate", "A: int, B: int, Operation: string", "int", "Performs calculation");
});
```

### 2. Add the Component

In your Blazor page or component:

```razor
@using Nesco.SignalRCommunicator.Client.Dashboard.Components

<SignalRClientDashboard
    Username="@currentUser"
    UserManagementHubUrl="/hubs/usermanagement"
    CommunicatorHubUrl="/hubs/communicator"
    ServerDashboardUrl="/signalr-dashboard" />
```

### 3. Add Imports

In your `_Imports.razor`:

```razor
@using Nesco.SignalRCommunicator.Client.Dashboard
@using Nesco.SignalRCommunicator.Client.Dashboard.Components
@using Nesco.SignalRCommunicator.Client.Dashboard.Services
```

## Component Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Username` | `string?` | `null` | Username to display in the header |
| `UserManagementHubUrl` | `string` | `/hubs/usermanagement` | User Management Hub URL to display |
| `CommunicatorHubUrl` | `string` | `/hubs/communicator` | Communicator Hub URL to display |
| `ServerDashboardUrl` | `string` | `/signalr-dashboard` | Server dashboard URL for tips |
| `AutoReconnectDelays` | `string` | `Retry delays: 0s, 2s, 5s, 10s, 30s` | Auto-reconnect delays to display |

## Services

### IClientMethodRegistry

Register methods that can be invoked by the server:

```csharp
public interface IClientMethodRegistry
{
    void RegisterMethod(string name, string parameters, string returnType, string description, bool isDefault = false);
    IEnumerable<ClientMethod> GetMethods();
    void Clear();
}
```

### IMethodInvocationLogger

Log method invocations for display in the dashboard:

```csharp
public interface IMethodInvocationLogger
{
    event Action? OnLogAdded;
    void Log(string methodName, string? parameter, string? result, string? error, bool success, TimeSpan duration);
    IEnumerable<MethodInvocationLog> GetLogs();
    void Clear();
    int MaxEntries { get; }
}
```

## Dependencies

- Nesco.SignalRCommunicator.Client
- Nesco.SignalRUserManagement.Client
- Microsoft.AspNetCore.Components.Web
- Microsoft.AspNetCore.Components.WebAssembly

## Requirements

- .NET 10.0+

## License

MIT License - See LICENSE for details.
