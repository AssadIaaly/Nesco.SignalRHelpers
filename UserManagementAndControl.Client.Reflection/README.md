# SignalR Reflection-Based Client Example

This project demonstrates the **reflection-based handler discovery** approach for handling SignalR method invocations from the server. Instead of manually routing methods with switch statements, handlers are automatically discovered and invoked based on method names.

## Quick Start

### 1. Install the Package

Reference the `Nesco.SignalRUserManagement.Client` package in your project.

### 2. Register Services in Program.cs

```csharp
using Nesco.SignalRUserManagement.Client.Handlers;

// Simplest usage - scans entry assembly automatically
builder.Services.AddSignalRUserManagementClientWithHandlers();
```

That's it! The library automatically scans your assembly for handler classes.

### 3. Create a Handler Class

Create a class that implements `ISignalRHandler`. Each public method becomes a SignalR method handler:

```csharp
using Nesco.SignalRUserManagement.Client.Handlers;

public class MySignalRHandlers : ISignalRHandler
{
    private readonly ILogger<MySignalRHandlers> _logger;

    // Constructor injection works!
    public MySignalRHandlers(ILogger<MySignalRHandlers> logger)
    {
        _logger = logger;
    }

    // Parameterless method - no request DTO needed!
    public Task<object?> Ping()
    {
        _logger.LogInformation("Ping received");
        return Task.FromResult<object?>(new { message = "Pong" });
    }

    // Method with parameter - auto-deserialized
    public Task<object?> Echo(EchoRequest request)
    {
        return Task.FromResult<object?>(new { echo = request.Message });
    }

    // Another parameterless method
    public Task<object?> GetClientInfo()
    {
        return Task.FromResult<object?>(new { platform = "Blazor" });
    }
}

// Only define DTOs for methods that need parameters
public class EchoRequest { public string Message { get; set; } = ""; }
```

## Registration Options

```csharp
// Option 1: Simplest - scans entry assembly automatically
builder.Services.AddSignalRUserManagementClientWithHandlers();

// Option 2: With client options
builder.Services.AddSignalRUserManagementClientWithHandlers(
    client => client.MaxDirectDataSizeBytes = 64 * 1024
);

// Option 3: Specify assembly explicitly
builder.Services.AddSignalRUserManagementClientWithHandlers(
    typeof(Program).Assembly,
    client => client.MaxDirectDataSizeBytes = 64 * 1024
);

// Option 4: Full control over handler options
builder.Services.AddSignalRUserManagementClientWithHandlers(
    handlers =>
    {
        handlers.AssembliesToScan.Add(typeof(Program).Assembly);
        handlers.HandlerLifetime = ServiceLifetime.Transient;
    },
    client => client.MaxDirectDataSizeBytes = 64 * 1024
);
```

## How It Works

1. At startup, the library scans the entry assembly (or specified assemblies) for classes implementing `ISignalRHandler` (or decorated with `[SignalRHandler]`)
2. Public methods in these classes are registered as SignalR method handlers
3. When the server invokes a method (e.g., `"Ping"`), the library:
   - Finds the handler method with matching name
   - Creates a scope and resolves the handler from DI
   - If the method has a parameter, deserializes the incoming data to the expected type
   - If the method has no parameters, invokes it directly
   - Returns the result

## Advanced Features

### Custom Method Names

Use `[SignalRMethod]` to map a different SignalR method name:

```csharp
[SignalRMethod("ServerPing")]
public Task<object?> HandleServerPing()
{
    // This handles "ServerPing" from the server
    return Task.FromResult<object?>(new { pong = true });
}
```

### Excluding Methods

Use `[SignalRIgnore]` to exclude a public method from being registered:

```csharp
public Task<object?> PublicHandler() { ... }

[SignalRIgnore]
public void HelperMethod() { ... }  // Not registered as handler
```

### Alternative: Attribute-Based Discovery

Instead of implementing `ISignalRHandler`, you can use the `[SignalRHandler]` attribute:

```csharp
[SignalRHandler]
public class MyHandlers
{
    public Task<object?> Ping() { ... }
}
```

## Project Structure

```
UserManagementAndControl.Client.Reflection/
├── Program.cs                          # Service registration (one line!)
├── SignalRHandlers/
│   └── AppSignalRHandlers.cs          # Handler implementation
├── Pages/
│   ├── Home.razor                      # Main page with connection status
│   └── Login.razor                     # Login page
└── README.md                           # This file
```

## Comparison: Old vs New Approach

### Old Approach (IMethodExecutor with switch)

```csharp
public class ClientMethodExecutor : IMethodExecutor
{
    public async Task<object?> ExecuteAsync(string methodName, object? parameter)
    {
        return methodName switch
        {
            "Ping" => await HandlePingAsync(parameter),
            "Echo" => await HandleEchoAsync(parameter),
            "GetClientInfo" => await HandleGetClientInfoAsync(parameter),
            // ... more cases
            _ => throw new NotSupportedException($"Method '{methodName}' not supported")
        };
    }

    private Task<object?> HandlePingAsync(object? parameter)
    {
        var request = ParameterParser.Parse<PingRequest>(parameter);  // Manual parsing!
        return Task.FromResult<object?>(new { pong = true });
    }
    // ... more private methods with manual parameter parsing
}

// Need empty DTOs for parameterless methods!
public class PingRequest { }
```

### New Approach (ISignalRHandler with reflection)

```csharp
public class MyHandlers : ISignalRHandler
{
    // No parameters? No problem! Just don't add any.
    public Task<object?> Ping() => Task.FromResult<object?>(new { pong = true });

    // Need parameters? Add them and they're auto-deserialized.
    public Task<object?> Echo(EchoRequest req) => Task.FromResult<object?>(new { echo = req.Message });

    // Another parameterless method
    public Task<object?> GetClientInfo() => Task.FromResult<object?>(new { platform = "Blazor" });
}

// Only define DTOs for methods that actually need parameters
public class EchoRequest { public string Message { get; set; } = ""; }
```

**Benefits:**
- Less boilerplate code
- Method names ARE the SignalR method names
- Parameters are automatically deserialized to the correct type
- **No empty request DTOs needed for parameterless methods!**
- Full DI support in handler constructors
- Easier to maintain and extend

## Error Handling

When a method is invoked that has no registered handler, the library:
1. Logs an error with the list of registered methods
2. Throws `NotSupportedException`
3. The exception is caught and returned as an error response to the server

## Testing

1. Run the server at `http://localhost:5000`
2. Run this client
3. Login with valid credentials
4. Go to server's SignalR test page at `http://localhost:5000/signalr-test`
5. Find this client and invoke methods like `Ping`, `Echo`, `Calculate`, etc.
6. Check browser console for handler execution logs
