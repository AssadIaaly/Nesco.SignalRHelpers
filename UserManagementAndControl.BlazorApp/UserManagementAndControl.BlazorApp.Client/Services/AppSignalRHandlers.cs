using Nesco.SignalRUserManagement.Client.Handlers;

namespace UserManagementAndControl.BlazorApp.Client.Services;

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

    public AppSignalRHandlers(ILogger<AppSignalRHandlers> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles "Ping" from the server - simple connectivity check.
    /// </summary>
    public Task<object?> Ping()
    {
        _logger.LogInformation("Ping received from server");
        return Task.FromResult<object?>(new
        {
            message = "Pong",
            timestamp = DateTime.UtcNow,
            platform = "Blazor WebAssembly"
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
            title = request.Title,
            message = request.Message
        });
    }

    /// <summary>
    /// Handles "GetClientInfo" from the server - returns client platform info.
    /// </summary>
    public Task<object?> GetClientInfo()
    {
        _logger.LogInformation("GetClientInfo request received");
        return Task.FromResult<object?>(new
        {
            platform = "Blazor WebAssembly",
            framework = ".NET 10",
            userAgent = "UserManagementAndControl.BlazorApp.Client",
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
            "%" => request.B != 0 ? request.A % request.B : double.NaN,
            "^" => Math.Pow(request.A, request.B),
            _ => throw new ArgumentException($"Unknown operation: {request.Operation}")
        };

        return Task.FromResult<object?>(new
        {
            a = request.A,
            b = request.B,
            operation = request.Operation,
            result,
            calculatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles "GetSystemInfo" from the server - returns system information
    /// </summary>
    public Task<object?> GetSystemInfo()
    {
        _logger.LogInformation("GetSystemInfo request received");
        return Task.FromResult<object?>(new
        {
            environment = "Browser",
            platform = "WebAssembly",
            osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles "GetRandomNumbers" from the server - generates random numbers
    /// </summary>
    public Task<object?> GetRandomNumbers(RandomNumbersRequest request)
    {
        var count = Math.Clamp(request.Count, 1, 100);
        var min = request.Min;
        var max = request.Max;

        _logger.LogInformation("GetRandomNumbers: generating {Count} numbers between {Min} and {Max}", count, min, max);

        var numbers = new List<int>();
        for (var i = 0; i < count; i++)
        {
            numbers.Add(Random.Shared.Next(min, max + 1));
        }

        return Task.FromResult<object?>(new
        {
            count,
            min,
            max,
            numbers,
            sum = numbers.Sum(),
            average = numbers.Average(),
            generatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles "ReverseString" from the server - reverses a string
    /// </summary>
    public Task<object?> ReverseString(ReverseStringRequest request)
    {
        _logger.LogInformation("ReverseString: {Input}", request.Input);
        var reversed = new string(request.Input.Reverse().ToArray());

        return Task.FromResult<object?>(new
        {
            original = request.Input,
            reversed,
            length = request.Input.Length
        });
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

public class CalculateRequest
{
    public double A { get; set; }
    public double B { get; set; }
    public string Operation { get; set; } = "+";
}

public class RandomNumbersRequest
{
    public int Count { get; set; } = 10;
    public int Min { get; set; } = 1;
    public int Max { get; set; } = 100;
}

public class ReverseStringRequest
{
    public string Input { get; set; } = string.Empty;
}

#endregion
