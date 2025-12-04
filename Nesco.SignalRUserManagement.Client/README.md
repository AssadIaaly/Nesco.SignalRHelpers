# Nesco.SignalRUserManagement.Client

Client-side library for connecting to SignalR User Management hubs. Supports Blazor WebAssembly, Blazor Server, and MAUI Blazor Hybrid applications.

## Features

- Automatic reconnection with configurable delays
- Method invocation handling via `IMethodExecutor`
- Large response support with automatic file upload
- Streaming multi-response support
- JWT authentication for cross-origin connections
- Works with Blazor WebAssembly, Blazor Server, and MAUI

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Client
```

## Quick Start

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

### Blazor WebAssembly

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
