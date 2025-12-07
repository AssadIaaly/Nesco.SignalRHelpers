using Nesco.SignalRUserManagement.Client.Authorization.Services;
using Nesco.SignalRUserManagement.Client.Handlers;

namespace UserManagementAndControl.Client.Reflection.SignalRHandlers;

/// <summary>
/// SignalR method handlers using reflection-based discovery.
///
/// Each public method in this class becomes a SignalR method handler.
/// The method name = the SignalR method name the server will invoke.
///
/// Methods can be parameterless or have a single parameter (auto-deserialized).
/// Dependencies are injected via constructor (standard DI).
/// </summary>
public class AppSignalRHandlers : ISignalRHandler
{
    private readonly ILogger<AppSignalRHandlers> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AppSignalRHandlers(
        ILogger<AppSignalRHandlers> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Handles "Ping" from the server - simple connectivity check.
    /// No parameters required!
    /// </summary>
    public Task<object?> Ping()
    {
        _logger.LogInformation("Ping received");
        return Task.FromResult<object?>(new
        {
            message = "Pong",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles "Echo" from the server - echoes back the message
    /// </summary>
    public Task<object?> Echo(EchoRequest request)
    {
        _logger.LogInformation("Echo received: {Message}", request.Message);
        return Task.FromResult<object?>(new
        {
            echo = request.Message,
            receivedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles "Alert" from the server - acknowledges an alert
    /// </summary>
    public Task<object?> Alert(AlertRequest request)
    {
        _logger.LogInformation("Alert received: {Title} - {Message}", request.Title, request.Message);
        return Task.FromResult<object?>(new
        {
            acknowledged = true,
            title = request.Title
        });
    }

    /// <summary>
    /// Handles "Navigate" from the server - acknowledges navigation request
    /// </summary>
    public Task<object?> Navigate(NavigateRequest request)
    {
        _logger.LogInformation("Navigate request: {Url}", request.Url);
        return Task.FromResult<object?>(new
        {
            willNavigate = true,
            url = request.Url
        });
    }

    /// <summary>
    /// Handles "GetClientInfo" from the server - returns client platform info.
    /// No parameters required!
    /// </summary>
    public Task<object?> GetClientInfo()
    {
        _logger.LogInformation("GetClientInfo request received");
        return Task.FromResult<object?>(new
        {
            platform = "Blazor WebAssembly",
            userAgent = "UserManagementAndControl.Client.Reflection",
            timestamp = DateTime.UtcNow,
            timezone = TimeZoneInfo.Local.DisplayName
        });
    }

    /// <summary>
    /// Handles "Calculate" from the server - performs arithmetic
    /// </summary>
    public Task<object?> Calculate(CalculateRequest request)
    {
        _logger.LogInformation("Calculate: {A} {Op} {B}", request.A, request.Operation, request.B);

        double result = request.Operation switch
        {
            "+" => request.A + request.B,
            "-" => request.A - request.B,
            "*" => request.A * request.B,
            "/" => request.B != 0 ? request.A / request.B : double.NaN,
            _ => throw new ArgumentException($"Unknown operation: {request.Operation}")
        };

        return Task.FromResult<object?>(new
        {
            a = request.A,
            b = request.B,
            operation = request.Operation,
            result
        });
    }

    /// <summary>
    /// Handles "GetLargeData" from the server - generates large dataset
    /// (tests file upload for responses exceeding MaxDirectDataSizeBytes)
    /// </summary>
    public Task<object?> GetLargeData(LargeDataRequest request)
    {
        var itemCount = request.ItemCount > 0 ? request.ItemCount : 1000;
        _logger.LogInformation("GetLargeData: generating {Count} items", itemCount);

        var items = new List<LargeDataItem>();
        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new LargeDataItem
            {
                Id = i + 1,
                Name = $"Item_{i + 1}_{Guid.NewGuid():N}",
                Description = $"Description for item {i + 1}. Lorem ipsum dolor sit amet.",
                Value = Math.Round(Random.Shared.NextDouble() * 10000, 2),
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Tags = [$"tag{i % 10}", $"category{i % 5}", "generated"]
            });
        }

        var totalSize = System.Text.Json.JsonSerializer.Serialize(items).Length;
        _logger.LogInformation("GetLargeData response: {Size} bytes", totalSize);

        return Task.FromResult<object?>(new
        {
            itemCount = items.Count,
            generatedAt = DateTime.UtcNow,
            approximateSizeBytes = totalSize,
            items
        });
    }

    /// <summary>
    /// Handles "Logout" from the server - logs out the user.
    /// No parameters required!
    /// </summary>
    public async Task<object?> Logout()
    {
        _logger.LogInformation("Logout request from server");
        using var scope = _serviceProvider.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        await authService.LogoutAsync();

        return new { loggedOut = true, timestamp = DateTime.UtcNow };
    }
}

#region Request DTOs

public class EchoRequest
{
    public string Message { get; set; } = string.Empty;
}

public class AlertRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class NavigateRequest
{
    public string Url { get; set; } = string.Empty;
}

public class CalculateRequest
{
    public double A { get; set; }
    public double B { get; set; }
    public string Operation { get; set; } = "+";
}

public class LargeDataRequest
{
    public int ItemCount { get; set; } = 1000;
}

public class LargeDataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string[] Tags { get; set; } = [];
}

#endregion
