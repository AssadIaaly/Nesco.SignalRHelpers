# Nesco.SignalRUserManagement.Server

Server-side library for managing SignalR user connections with database persistence and bi-directional method invocation. Track connected users, invoke methods on clients, and receive streaming responses.

## Features

- Automatic connection tracking with database persistence
- Bi-directional method invocation (server-to-client with responses)
- Streaming multi-response support for invoking methods on multiple clients
- Large response handling via file upload
- Built-in dashboard component for testing and monitoring
- Ping/pong mechanism for stale connection validation
- Support for multiple connections per user
- Works with both cookie and JWT authentication

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Server
```

## Quick Start

### 1. Add UserConnection to your DbContext

Your DbContext must implement `IUserConnectionDbContext`:

```csharp
using Microsoft.EntityFrameworkCore;
using Nesco.SignalRUserManagement.Server.Models;

public class ApplicationDbContext : DbContext, IUserConnectionDbContext
{
    public DbSet<UserConnection> UserConnections { get; set; } = null!;

    // ... your other DbSets
}
```

### 2. Create and apply migration

```bash
dotnet ef migrations add AddUserConnections
dotnet ef database update
```

### 3. Configure services in Program.cs

```csharp
using Nesco.SignalRUserManagement.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR User Management with method invocation support
builder.Services.AddSignalRUserManagement<ApplicationDbContext>(options =>
{
    options.EnableCommunicator = true; // Enable server-to-client method invocation
    options.AutoDeleteTempFiles = true; // Auto-cleanup uploaded response files
});

var app = builder.Build();

// Map the hub and file upload endpoint
app.MapSignalRUserManagement<ApplicationDbContext>();

app.Run();
```

---

## Server Configuration

### Full Configuration Example

```csharp
builder.Services.AddSignalRUserManagement<ApplicationDbContext>(options =>
{
    // Enable method invocation on clients
    options.EnableCommunicator = true;

    // Large response handling
    options.MaxDirectDataSizeBytes = 32 * 1024; // 32KB threshold
    options.AutoDeleteTempFiles = true; // Delete files after reading
    options.TempFolder = "signalr-temp"; // Temp folder for uploads

    // Timeouts
    options.RequestTimeoutSeconds = 30; // Method invocation timeout
    options.MaxConcurrentRequests = 100; // Max concurrent invocations
});
```

### Mapping Endpoints

```csharp
// Maps both the hub and file upload endpoint
app.MapSignalRUserManagement<ApplicationDbContext>();

// Or map separately with custom paths:
app.MapUserManagementHub<ApplicationDbContext>("/hubs/usermanagement");
app.MapSignalRFileUpload(); // Maps to /api/signalr/upload
```

---

## Using the Dashboard Component

The library includes a built-in Blazor dashboard component for testing and monitoring.

### 1. Add the component to your app

```razor
@page "/signalr-dashboard"
@using Nesco.SignalRUserManagement.Server.Components

<SignalRDashboard />
```

### 2. Features

The dashboard provides:
- **Connected Users**: View all connected users and their connections
- **Method Invocation**: Test invoking methods on clients
- **Streaming Results**: See responses as they arrive from multiple clients
- **Ping All**: Quick connectivity check for all users

---

## Invoking Methods on Clients

### Using ISignalRUserManagementService

```csharp
public class NotificationService
{
    private readonly ISignalRUserManagementService _signalR;

    public NotificationService(ISignalRUserManagementService signalR)
    {
        _signalR = signalR;
    }

    // Invoke on all connected users
    public async Task<SignalRResponse> PingAll()
    {
        return await _signalR.InvokeOnAllConnectedAsync("Ping", null);
    }

    // Invoke on specific user
    public async Task<SignalRResponse> GetUserInfo(string userId)
    {
        return await _signalR.InvokeOnUserAsync(userId, "GetClientInfo", null);
    }

    // Invoke on specific connection
    public async Task<SignalRResponse> SendAlert(string connectionId, object alert)
    {
        return await _signalR.InvokeOnConnectionAsync(connectionId, "Alert", alert);
    }

    // Check if user is connected
    public bool IsOnline(string userId)
    {
        return _signalR.IsUserConnected(userId);
    }
}
```

### Streaming Responses from Multiple Clients

When invoking methods on multiple clients, use streaming to receive responses as they arrive:

```csharp
public async Task StreamPingResults()
{
    await foreach (var response in _signalR.InvokeOnAllConnectedStreamingAsync("Ping", null))
    {
        Console.WriteLine($"Response from {response.ConnectionId}: {response.Success}");

        if (response.Success && response.Response?.JsonData != null)
        {
            // Process each response as it arrives
            ProcessResponse(response.Response.JsonData);
        }
    }
}
```

### Available Streaming Methods

```csharp
// Stream responses from all connected users
IAsyncEnumerable<ClientResponse> InvokeOnAllConnectedStreamingAsync(
    string methodName, object? parameter, CancellationToken ct = default);

// Stream responses from a specific user (all their connections)
IAsyncEnumerable<ClientResponse> InvokeOnUserStreamingAsync(
    string userId, string methodName, object? parameter, CancellationToken ct = default);

// Stream responses from multiple users
IAsyncEnumerable<ClientResponse> InvokeOnUsersStreamingAsync(
    IEnumerable<string> userIds, string methodName, object? parameter, CancellationToken ct = default);

// Stream responses from specific connections
IAsyncEnumerable<ClientResponse> InvokeOnConnectionsStreamingAsync(
    IEnumerable<string> connectionIds, string methodName, object? parameter, CancellationToken ct = default);
```

---

## Response Types

### SignalRResponse

```csharp
public class SignalRResponse
{
    public SignalRResponseType ResponseType { get; set; }
    public object? JsonData { get; set; }      // For direct JSON responses
    public string? FilePath { get; set; }       // For file-based responses
    public string? ErrorMessage { get; set; }   // For errors
}

public enum SignalRResponseType
{
    Null,       // No data
    JsonObject, // Direct JSON data
    FilePath,   // Response stored in file (large data)
    Error       // Error occurred
}
```

### ClientResponse (for streaming)

```csharp
public class ClientResponse
{
    public string ConnectionId { get; set; }
    public string? UserId { get; set; }
    public SignalRResponse? Response { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAt { get; set; }
}
```

---

## JWT Authentication Setup

For standalone clients (WebAssembly, MAUI) connecting cross-origin:

### 1. Add CORS Policy

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5001") // Client app URL
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

app.UseCors();
```

### 2. Configure JWT Bearer Authentication

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "YourIssuer",
            ValidAudience = "YourAudience",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("YourSecretKey"))
        };

        // Allow JWT token from query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

### 3. Add Custom UserIdProvider

```csharp
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? connection.User?.FindFirst("sub")?.Value;
    }
}

// Register in Program.cs
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
```

---

## Built-in REST API

The library includes a `ConnectionsController` for querying connected users:

### Enable Controllers

```csharp
builder.Services.AddControllers();
app.MapControllers();
```

### Available Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/connections` | Get all connected users with their connections |
| GET | `/api/connections?purgeStale=false` | Get connections without purging stale ones |
| POST | `/api/connections/purge` | Purge stale connections, returns count purged |
| GET | `/api/connections/count` | Get total number of active connections |
| GET | `/api/connections/users/count` | Get number of unique connected users |
| GET | `/api/connections/users/{userId}/status` | Check if a specific user is connected |
| GET | `/api/connections/users/{userId}` | Get connections for a specific user |

---

## Database Model

The library uses a `UserConnection` model that stores connection information:

```csharp
public class UserConnection
{
    [Key]
    public string ConnectionId { get; set; }

    [Required]
    public string UserId { get; set; }

    /// <summary>
    /// The username associated with this connection (resolved at connection time from claims)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Extra data that can be stored with the connection (e.g., JSON metadata)
    /// </summary>
    public string? Extra { get; set; }

    public DateTime ConnectedAt { get; set; }
}
```

### Username Resolution

The `Username` is automatically populated when a user connects by reading from common claim types:
- `ClaimTypes.Name`
- `name` claim
- `preferred_username` claim
- `Identity.Name`

This means user names are available in the dashboard and API without any additional configuration.

---

## Complete Server Setup Example

```csharp
using Nesco.SignalRUserManagement.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

// Add CORS for cross-origin clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add authentication (JWT + Cookies)
builder.Services.AddAuthentication()
    .AddJwtBearer(options => { /* ... */ });

// Add SignalR User Management
builder.Services.AddSignalRUserManagement<ApplicationDbContext>(options =>
{
    options.EnableCommunicator = true;
    options.AutoDeleteTempFiles = true;
});

// Add custom user ID provider
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapSignalRUserManagement<ApplicationDbContext>();

app.Run();
```

---

## Notes

- Connections are automatically tracked on connect/disconnect
- Stale connections are purged using ping/pong validation
- Large responses from clients are automatically handled via file upload
- Streaming responses allow real-time UI updates as clients respond
- The dashboard component uses Blazor-native expand/collapse (no JS required)
