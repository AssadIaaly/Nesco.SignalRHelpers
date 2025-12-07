using Nesco.SignalRUserManagement.Client.Authorization.Services;
using Nesco.SignalRUserManagement.Client.Handlers;
using Nesco.SignalRUserManagement.Core.Utilities;
using UserManagementAndControl.Client.Services;

namespace UserManagementAndControl.Client.SignalRHandlers;

/// <summary>
/// SignalR method handlers using the new reflection-based approach.
/// Each public method name corresponds to a SignalR method that can be invoked by the server.
/// Dependencies are injected via constructor.
/// </summary>
public class ClientSignalRHandlers : ISignalRHandler
{
    private readonly ILogger<ClientSignalRHandlers> _logger;
    private readonly MethodInvocationLogger _invocationLogger;
    private readonly IServiceProvider _serviceProvider;

    public ClientSignalRHandlers(
        ILogger<ClientSignalRHandlers> logger,
        MethodInvocationLogger invocationLogger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _invocationLogger = invocationLogger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Handles the "Ping" method from the server
    /// </summary>
    public Task<object?> Ping(PingRequest? request)
    {
        _logger.LogInformation("Ping received");
        return Task.FromResult<object?>(new { message = "Pong", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Handles the "Echo" method from the server
    /// </summary>
    public Task<object?> Echo(EchoRequest request)
    {
        _logger.LogInformation("Echo received: {Message}", request.Message);
        return Task.FromResult<object?>(new { echo = request.Message, receivedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Handles the "Alert" method from the server
    /// </summary>
    public Task<object?> Alert(AlertRequest request)
    {
        _logger.LogInformation("Alert received: {Title} - {Message}", request.Title, request.Message);
        return Task.FromResult<object?>(new { acknowledged = true, title = request.Title });
    }

    /// <summary>
    /// Handles the "Navigate" method from the server
    /// </summary>
    public Task<object?> Navigate(NavigateRequest request)
    {
        _logger.LogInformation("Navigate request received: {Url}", request.Url);
        return Task.FromResult<object?>(new { willNavigate = true, url = request.Url });
    }

    /// <summary>
    /// Handles the "GetClientInfo" method from the server
    /// </summary>
    public Task<object?> GetClientInfo(EmptyRequest? request)
    {
        _logger.LogInformation("GetClientInfo request received");
        return Task.FromResult<object?>(new
        {
            platform = "Blazor WebAssembly",
            userAgent = "UserManagementAndControl.Client",
            timestamp = DateTime.UtcNow,
            timezone = TimeZoneInfo.Local.DisplayName
        });
    }

    /// <summary>
    /// Handles the "Calculate" method from the server
    /// </summary>
    public Task<object?> Calculate(CalculateRequest request)
    {
        _logger.LogInformation("Calculate request: {A} {Operation} {B}", request.A, request.Operation, request.B);

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
    /// Handles the "GetLargeData" method from the server
    /// </summary>
    public Task<object?> GetLargeData(LargeDataRequest request)
    {
        var itemCount = request.ItemCount > 0 ? request.ItemCount : 1000;

        _logger.LogInformation("GetLargeData request: generating {Count} items", itemCount);

        var items = new List<LargeDataItem>();
        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new LargeDataItem
            {
                Id = i + 1,
                Name = $"Item_{i + 1}_{Guid.NewGuid():N}",
                Description = $"This is a detailed description for item {i + 1}. It contains enough text to make the response larger. Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                Value = Math.Round(Random.Shared.NextDouble() * 10000, 2),
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Tags = [$"tag{i % 10}", $"category{i % 5}", "generated"],
                Metadata = new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["batch"] = i / 100,
                    ["randomValue"] = Random.Shared.Next(1, 1000)
                }
            });
        }

        var totalSize = System.Text.Json.JsonSerializer.Serialize(items).Length;
        _logger.LogInformation("GetLargeData response size: {Size} bytes ({Count} items)", totalSize, itemCount);

        return Task.FromResult<object?>(new
        {
            itemCount = items.Count,
            generatedAt = DateTime.UtcNow,
            approximateSizeBytes = totalSize,
            items
        });
    }

    /// <summary>
    /// Handles the "Logout" method from the server
    /// </summary>
    public async Task<object?> Logout(EmptyRequest? request)
    {
        _logger.LogInformation("Logout request received from server");
        using var scope = _serviceProvider.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        await authService.LogoutAsync();

        return new { loggedOut = true, timestamp = DateTime.UtcNow };
    }
}

#region Request DTOs

public class PingRequest
{
}

public class EmptyRequest
{
}

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
    public Dictionary<string, object> Metadata { get; set; } = new();
}

#endregion
