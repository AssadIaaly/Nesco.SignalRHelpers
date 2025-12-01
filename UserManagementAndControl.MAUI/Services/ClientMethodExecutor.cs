using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Utilities;

namespace UserManagementAndControl.MAUI.Services;

public class ClientMethodExecutor : IMethodExecutor
{
    private readonly ILogger<ClientMethodExecutor> _logger;
    private readonly MethodInvocationLogger _invocationLogger;

    public event Action<string, object?>? OnMethodInvoked;

    public ClientMethodExecutor(ILogger<ClientMethodExecutor> logger, MethodInvocationLogger invocationLogger)
    {
        _logger = logger;
        _invocationLogger = invocationLogger;
    }

    public async Task<object?> ExecuteAsync(string methodName, object? parameter)
    {
        _logger.LogInformation("Executing method: {MethodName}", methodName);
        OnMethodInvoked?.Invoke(methodName, parameter);

        var stopwatch = Stopwatch.StartNew();
        object? result = null;
        Exception? error = null;

        try
        {
            result = methodName switch
            {
                "Ping" => await HandlePingAsync(),
                "Echo" => await HandleEchoAsync(parameter),
                "Alert" => await HandleAlertAsync(parameter),
                "Navigate" => await HandleNavigateAsync(parameter),
                "GetClientInfo" => await HandleGetClientInfoAsync(),
                "Calculate" => await HandleCalculateAsync(parameter),
                "GetLargeData" => await HandleGetLargeDataAsync(parameter),
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported")
            };
            return result;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _invocationLogger.LogInvocation(methodName, parameter, result, stopwatch.Elapsed, error);
        }
    }

    private Task<object?> HandlePingAsync()
    {
        _logger.LogInformation("Ping received");
        return Task.FromResult<object?>(new { message = "Pong", timestamp = DateTime.UtcNow });
    }

    private Task<object?> HandleEchoAsync(object? parameter)
    {
        var request = ParameterParser.Parse<EchoRequest>(parameter);
        _logger.LogInformation("Echo received: {Message}", request.Message);
        return Task.FromResult<object?>(new { echo = request.Message, receivedAt = DateTime.UtcNow });
    }

    private Task<object?> HandleAlertAsync(object? parameter)
    {
        var request = ParameterParser.Parse<AlertRequest>(parameter);
        _logger.LogInformation("Alert received: {Title} - {Message}", request.Title, request.Message);
        return Task.FromResult<object?>(new { acknowledged = true, title = request.Title });
    }

    private Task<object?> HandleNavigateAsync(object? parameter)
    {
        var request = ParameterParser.Parse<NavigateRequest>(parameter);
        _logger.LogInformation("Navigate request received: {Url}", request.Url);
        return Task.FromResult<object?>(new { willNavigate = true, url = request.Url });
    }

    private Task<object?> HandleGetClientInfoAsync()
    {
        _logger.LogInformation("GetClientInfo request received");
        return Task.FromResult<object?>(new
        {
            platform = $"MAUI Blazor Hybrid ({DeviceInfo.Platform})",
            userAgent = "UserManagementAndControl.MAUI",
            timestamp = DateTime.UtcNow,
            deviceModel = DeviceInfo.Model,
            deviceManufacturer = DeviceInfo.Manufacturer,
            osVersion = DeviceInfo.VersionString,
            idiom = DeviceInfo.Idiom.ToString()
        });
    }

    private Task<object?> HandleCalculateAsync(object? parameter)
    {
        var request = ParameterParser.Parse<CalculateRequest>(parameter);
        _logger.LogInformation("Calculate request: {A} {Operation} {B}", request.A, request.Operation, request.B);

        double result = request.Operation switch
        {
            "+" => request.A + request.B,
            "-" => request.A - request.B,
            "*" => request.A * request.B,
            "/" => request.B != 0 ? request.A / request.B : double.NaN,
            _ => throw new ArgumentException($"Unknown operation: {request.Operation}")
        };

        return Task.FromResult<object?>(new { a = request.A, b = request.B, operation = request.Operation, result });
    }

    private Task<object?> HandleGetLargeDataAsync(object? parameter)
    {
        var request = ParameterParser.Parse<LargeDataRequest>(parameter);
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
                Tags = new[] { $"tag{i % 10}", $"category{i % 5}", "generated" },
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
}

public class EchoRequest { public string Message { get; set; } = string.Empty; }
public class AlertRequest { public string Title { get; set; } = string.Empty; public string Message { get; set; } = string.Empty; }
public class NavigateRequest { public string Url { get; set; } = string.Empty; }
public class CalculateRequest { public double A { get; set; } public double B { get; set; } public string Operation { get; set; } = "+"; }
public class LargeDataRequest { public int ItemCount { get; set; } = 1000; }
public class LargeDataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
