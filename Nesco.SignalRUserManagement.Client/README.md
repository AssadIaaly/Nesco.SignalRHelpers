# Nesco.SignalRUserManagement.Client

Client-side library for connecting to SignalR User Management hubs. Supports Blazor WebAssembly, Blazor Server, and MAUI Blazor Hybrid applications.

## Features

- **Reflection-based handler discovery** - Auto-discover handler methods from `ISignalRHandler` classes
- Automatic reconnection with configurable delays
- Method invocation handling via `IMethodExecutor` or reflection
- Large response support with automatic file upload
- Streaming multi-response support
- **Cookie authentication** for Blazor Web Apps
- JWT authentication for cross-origin connections
- Works with Blazor WebAssembly, Blazor Server, and MAUI

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Client
```

## Quick Start

There are two ways to handle server-initiated method calls:

1. **Reflection-based Handlers** (Recommended) - Auto-discovers handler methods using reflection
2. **Manual IMethodExecutor** - Manually route method calls with a switch statement

---

## Option 1: Reflection-based Handlers (Recommended)

The reflection-based approach automatically discovers handler methods from classes implementing `ISignalRHandler`. Method names in your handler class become the SignalR method names the server can invoke.

### 1. Create Handler Classes

```csharp
using Nesco.SignalRUserManagement.Client.Handlers;

public class AppSignalRHandlers : ISignalRHandler
{
    private readonly ILogger<AppSignalRHandlers> _logger;

    // Dependencies are injected via constructor (standard DI)
    public AppSignalRHandlers(ILogger<AppSignalRHandlers> logger)
    {
        _logger = logger;
    }

    // Method name "Ping" = SignalR method name "Ping"
    public Task<object?> Ping()
    {
        _logger.LogInformation("Ping received");
        return Task.FromResult<object?>(new
        {
            message = "Pong",
            timestamp = DateTime.UtcNow
        });
    }

    // Method with parameter - auto-deserialized from JSON
    public Task<object?> Echo(EchoRequest request)
    {
        _logger.LogInformation("Echo: {Message}", request.Message);
        return Task.FromResult<object?>(new
        {
            echo = request.Message,
            receivedAt = DateTime.UtcNow
        });
    }

    // Complex method with calculations
    public Task<object?> Calculate(CalculateRequest request)
    {
        double result = request.Operation switch
        {
            "+" => request.A + request.B,
            "-" => request.A - request.B,
            "*" => request.A * request.B,
            "/" => request.B != 0 ? request.A / request.B : double.NaN,
            _ => throw new ArgumentException($"Unknown operation: {request.Operation}")
        };

        return Task.FromResult<object?>(new { result });
    }

    public Task<object?> GetClientInfo()
    {
        return Task.FromResult<object?>(new
        {
            platform = "Blazor WebAssembly",
            framework = ".NET 9",
            timestamp = DateTime.UtcNow
        });
    }
}

// Request DTOs
public class EchoRequest { public string Message { get; set; } = string.Empty; }
public class CalculateRequest
{
    public double A { get; set; }
    public double B { get; set; }
    public string Operation { get; set; } = "+";
}
```

### 2. Configure Services

```csharp
using Nesco.SignalRUserManagement.Client.Handlers;

// Simplest usage - scans entry assembly automatically for ISignalRHandler implementations
builder.Services.AddSignalRUserManagementClientWithHandlers();

// With client options
builder.Services.AddSignalRUserManagementClientWithHandlers(options =>
{
    options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
    options.EnableFileUpload = true;
});

// Scan a specific assembly
builder.Services.AddSignalRUserManagementClientWithHandlers(
    typeof(MyHandlers).Assembly,
    options => { /* ... */ });

// Full control over handler discovery
builder.Services.AddSignalRUserManagementClientWithHandlers(
    handlers =>
    {
        handlers.AssembliesToScan.Add(typeof(MyHandlers).Assembly);
        handlers.HandlerLifetime = ServiceLifetime.Scoped;
    },
    client => { /* client options */ });
```

### 3. Alternative: Use [SignalRHandler] Attribute

Instead of implementing `ISignalRHandler`, you can use the `[SignalRHandler]` attribute:

```csharp
using Nesco.SignalRUserManagement.Client.Handlers;

[SignalRHandler]
public class NotificationHandlers
{
    private readonly ILogger<NotificationHandlers> _logger;

    public NotificationHandlers(ILogger<NotificationHandlers> logger)
    {
        _logger = logger;
    }

    public Task<object?> Alert(AlertRequest request)
    {
        _logger.LogInformation("Alert: {Title}", request.Title);
        return Task.FromResult<object?>(new { acknowledged = true });
    }
}
```

### 4. Multiple Handler Classes

You can have multiple handler classes - all are discovered automatically:

```csharp
// Handlers/SystemHandlers.cs
public class SystemHandlers : ISignalRHandler
{
    public Task<object?> Ping() => Task.FromResult<object?>("Pong");
    public Task<object?> GetSystemInfo() => Task.FromResult<object?>(new { os = "Browser" });
}

// Handlers/NotificationHandlers.cs
public class NotificationHandlers : ISignalRHandler
{
    public Task<object?> Alert(AlertRequest r) => Task.FromResult<object?>(new { ok = true });
    public Task<object?> Toast(ToastRequest r) => Task.FromResult<object?>(new { shown = true });
}

// Handlers/DataHandlers.cs
[SignalRHandler]
public class DataHandlers
{
    public Task<object?> GetData() => Task.FromResult<object?>(new { items = new[] { 1, 2, 3 } });
}
```

### Benefits of Reflection-based Handlers

- **Clean code**: No switch statements or manual routing
- **Auto-discovery**: Handlers are found automatically at startup
- **Dependency injection**: Full DI support in handler constructors
- **Type-safe parameters**: Parameters are auto-deserialized to strongly-typed DTOs
- **Multiple handlers**: Split handlers across multiple classes for organization
- **Flexible discovery**: Use `ISignalRHandler` interface or `[SignalRHandler]` attribute

---

## Option 2: Manual IMethodExecutor

For more control or simpler scenarios, implement `IMethodExecutor` directly:

### 1. Create a Method Executor

Implement `IMethodExecutor` to handle server-initiated method calls:

```csharp
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Utilities;

public class ClientMethodExecutor : IMethodExecutor
{
    public async Task<object?> ExecuteAsync(string methodName, object? parameter)
    {
        return methodName switch
        {
            "Ping" => new { message = "Pong", timestamp = DateTime.UtcNow },
            "Echo" => HandleEcho(parameter),
            "GetClientInfo" => GetClientInfo(),
            _ => throw new NotSupportedException($"Method '{methodName}' is not supported")
        };
    }

    private object HandleEcho(object? parameter)
    {
        var request = ParameterParser.Parse<EchoRequest>(parameter);
        return new { echo = request.Message, receivedAt = DateTime.UtcNow };
    }

    private object GetClientInfo()
    {
        return new
        {
            platform = "Blazor WebAssembly",
            timestamp = DateTime.UtcNow
        };
    }
}

public class EchoRequest { public string Message { get; set; } = string.Empty; }
```

### 2. Configure Services

```csharp
using Nesco.SignalRUserManagement.Client.Extensions;

builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
{
    options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB threshold for file upload
    options.EnableFileUpload = true; // Enable for large responses
});
```

### 3. Connect to the Hub

```csharp
@inject UserConnectionClient ConnectionClient

await ConnectionClient.StartAsync(
    "https://server.com/hubs/usermanagement",
    accessTokenProvider: () => Task.FromResult(myJwtToken));
```

---

## Platform-Specific Setup

### Blazor Web App with Cookie Authentication

For Blazor Web Apps using ASP.NET Core Identity with cookie authentication, use `StartWithCookiesAsync` instead of `StartAsync`:

#### Program.cs (Client Project)

```csharp
using Nesco.SignalRUserManagement.Client.Handlers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// Use reflection-based handler discovery
builder.Services.AddSignalRUserManagementClientWithHandlers(options =>
{
    options.EnableFileUpload = false;
});

await builder.Build().RunAsync();
```

#### SignalRHandlers.cs (Create handler class)

```csharp
using Nesco.SignalRUserManagement.Client.Handlers;

public class AppSignalRHandlers : ISignalRHandler
{
    private readonly ILogger<AppSignalRHandlers> _logger;

    public AppSignalRHandlers(ILogger<AppSignalRHandlers> logger)
    {
        _logger = logger;
    }

    public Task<object?> Ping()
    {
        _logger.LogInformation("Ping received");
        return Task.FromResult<object?>(new
        {
            message = "Pong",
            timestamp = DateTime.UtcNow
        });
    }

    public Task<object?> GetClientInfo()
    {
        return Task.FromResult<object?>(new
        {
            platform = "Blazor WebAssembly",
            timestamp = DateTime.UtcNow
        });
    }

    // Add more methods as needed - method names become SignalR method names
}
```

#### Connection Component

```razor
@page "/signalr-demo"
@rendermode InteractiveWebAssembly
@using Nesco.SignalRUserManagement.Client.Services
@inject UserConnectionClient ConnectionClient
@inject NavigationManager NavigationManager
@attribute [Authorize]

<h1>SignalR Demo</h1>

<p>Status: @(ConnectionClient.IsConnected ? "Connected" : "Disconnected")</p>

@if (!ConnectionClient.IsConnected)
{
    <button @onclick="ConnectAsync">Connect</button>
}
else
{
    <button @onclick="DisconnectAsync">Disconnect</button>
}

@code {
    private async Task ConnectAsync()
    {
        // Build the hub URL from current base URI
        var hubUrl = NavigationManager.ToAbsoluteUri("/hubs/usermanagement").ToString();

        // Connect using cookies (authentication handled automatically by browser)
        await ConnectionClient.StartWithCookiesAsync(hubUrl);
    }

    private async Task DisconnectAsync()
    {
        await ConnectionClient.StopAsync();
    }
}
```

#### How Cookie Authentication Works

When using `StartWithCookiesAsync`:
1. The browser automatically includes authentication cookies with SignalR requests
2. No need to manually manage JWT tokens
3. Works seamlessly with ASP.NET Core Identity
4. The server-side hub receives the authenticated user via `Context.User`

**Note:** Cookie authentication only works for same-origin connections (client and server on the same domain). For cross-origin scenarios, use JWT authentication instead.

---

### Blazor WebAssembly (Standalone with JWT)

#### Program.cs

```csharp
using Nesco.SignalRUserManagement.Client.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

const string serverUrl = "http://localhost:5000";

// Configure HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(serverUrl)
});

// Add SignalR User Management Client
builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
{
    options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
});

await builder.Build().RunAsync();
```

#### Home.razor (Connection Example)

```razor
@inject UserConnectionClient ConnectionClient
@implements IDisposable

<div>
    Status: @(ConnectionClient.IsConnected ? "Connected" : "Disconnected")
</div>

@code {
    protected override async Task OnInitializedAsync()
    {
        ConnectionClient.ConnectionStatusChanged += OnConnectionChanged;

        if (!ConnectionClient.IsConnected)
        {
            await ConnectionClient.StartAsync("http://localhost:5000/hubs/usermanagement");
        }
    }

    private async void OnConnectionChanged(bool connected)
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ConnectionClient.ConnectionStatusChanged -= OnConnectionChanged;
    }
}
```

---

### Blazor Server (as Client)

Blazor Server apps can act as clients to another SignalR server.

#### Program.cs

```csharp
using Nesco.SignalRUserManagement.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

const string serverUrl = "http://localhost:5000";

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HttpClient
builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri(serverUrl);
});

// Register default HttpClient for file upload service
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("ServerApi");
});

// Add SignalR User Management Client with file upload
builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
{
    options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
    options.EnableFileUpload = true;
});

var app = builder.Build();
app.Run();
```

#### Key Differences from WebAssembly

- Use `IHttpClientFactory` for HTTP clients
- Register a default `HttpClient` using the factory for `DefaultFileUploadService`

---

### MAUI Blazor Hybrid

#### MauiProgram.cs

```csharp
using Nesco.SignalRUserManagement.Client.Extensions;

public static class MauiProgram
{
    // For Android emulator use 10.0.2.2, for physical device use your machine's IP
#if ANDROID
    private const string ServerUrl = "http://10.0.2.2:5000";  // Emulator
    // private const string ServerUrl = "http://192.168.1.100:5000"; // Physical device
#else
    private const string ServerUrl = "http://localhost:5000";
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        builder.Services.AddMauiBlazorWebView();

        // Configure HttpClient
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(ServerUrl)
        });

        // Add SignalR User Management Client
        builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
        {
            options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
            options.EnableFileUpload = true;
        });

        return builder.Build();
    }
}
```

#### Android Network Configuration

For Android physical devices:
1. Find your dev machine's IP: `ipconfig` (Windows) or `ifconfig` (Mac/Linux)
2. Ensure phone and dev machine are on the same network
3. Update `ServerUrl` to use your machine's IP
4. Make sure the server listens on `0.0.0.0` or the specific IP

---

## Configuration Options

```csharp
builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
{
    // Connection settings
    options.HubUrl = "https://server.com/hubs/usermanagement"; // Optional default URL
    options.ReconnectDelaysSeconds = [0, 2, 5, 10, 30]; // Reconnect backoff

    // Large response handling
    options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB - responses larger than this use file upload
    options.EnableFileUpload = true; // Enable file upload for large responses
    options.FileUploadRoute = "api/signalr/upload"; // Server upload endpoint
    options.TempFolder = "signalr-temp"; // Server-side temp folder
});
```

---

## API Reference

### UserConnectionClient

```csharp
// Properties
bool IsConnected { get; }
string? ConnectionId { get; }
HubConnectionState State { get; }
HubConnection? Connection { get; }

// Events
event Action<bool>? ConnectionChanged;
event Action<bool>? ConnectionStatusChanged;
event Action? Reconnecting;
event Action<string?>? Reconnected;

// Methods
Task StartAsync(
    string hubUrl,
    Func<Task<string>>? accessTokenProvider = null,
    Action<HttpConnectionOptions>? configureOptions = null);

Task StartWithCookiesAsync(
    string hubUrl,
    Action<HttpConnectionOptions>? configureOptions = null);

Task StopAsync();

void On<T>(string methodName, Action<T> handler);
void On<T>(string methodName, Func<T, Task> handler);

Task SendAsync(string methodName, object? arg = null);
Task<T> InvokeAsync<T>(string methodName, object? arg = null);
```

### IMethodExecutor

Implement this interface to handle server-initiated method calls:

```csharp
public interface IMethodExecutor
{
    Task<object?> ExecuteAsync(string methodName, object? parameter);
}
```

### ParameterParser

Utility for parsing method parameters:

```csharp
var request = ParameterParser.Parse<MyRequest>(parameter);
```

---

## Large Response Handling

When a method returns data larger than `MaxDirectDataSizeBytes`, the client automatically:

1. Serializes the response to JSON
2. Uploads it to the server via HTTP POST
3. Returns the file path to the server
4. Server reads the file and processes the response

This enables returning large datasets without hitting SignalR message size limits.

### Example: Large Data Method

```csharp
public class ClientMethodExecutor : IMethodExecutor
{
    public async Task<object?> ExecuteAsync(string methodName, object? parameter)
    {
        return methodName switch
        {
            "GetLargeData" => GenerateLargeData(parameter),
            // ...
        };
    }

    private object GenerateLargeData(object? parameter)
    {
        var request = ParameterParser.Parse<LargeDataRequest>(parameter);
        var items = new List<DataItem>();

        for (var i = 0; i < request.ItemCount; i++)
        {
            items.Add(new DataItem
            {
                Id = i,
                Name = $"Item_{i}",
                Description = "Lorem ipsum..."
            });
        }

        return new { itemCount = items.Count, items };
    }
}
```

---

## Connection Lifecycle

### States

| State | Description |
|-------|-------------|
| `Disconnected` | No connection to the server |
| `Connecting` | Connection is being established |
| `Connected` | Successfully connected |
| `Reconnecting` | Connection lost, attempting to reconnect |

### Events

```csharp
ConnectionClient.ConnectionStatusChanged += (connected) =>
{
    Console.WriteLine(connected ? "Connected!" : "Disconnected");
};

ConnectionClient.Reconnecting += () =>
{
    Console.WriteLine("Reconnecting...");
};

ConnectionClient.Reconnected += (connectionId) =>
{
    Console.WriteLine($"Reconnected: {connectionId}");
};
```

---

## Notes

- The client automatically responds to server pings for connection validation
- Reconnection happens automatically with configurable backoff delays
- For cross-origin connections, ensure the server has CORS configured
- JWT tokens are passed via query string for SignalR WebSocket connections
- File upload requires the server to have the upload endpoint configured
