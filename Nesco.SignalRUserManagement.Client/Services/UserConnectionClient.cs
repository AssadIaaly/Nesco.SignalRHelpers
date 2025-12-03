using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Models;
using Nesco.SignalRUserManagement.Core.Options;
using System.Text.Json;

namespace Nesco.SignalRUserManagement.Client.Services;

/// <summary>
/// Client for managing SignalR hub connection with automatic reconnection
/// </summary>
public class UserConnectionClient : IAsyncDisposable
{
    private readonly ILogger<UserConnectionClient> _logger;
    private readonly UserManagementOptions _options;
    private readonly CommunicatorOptions _communicatorOptions;
    private readonly IMethodExecutor? _methodExecutor;
    private readonly IServiceProvider _serviceProvider;
    private HubConnection? _connection;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>
    /// Raised when connection status changes
    /// </summary>
    public event Action<bool>? ConnectionChanged;

    /// <summary>
    /// Raised when connection status changes (alias for ConnectionChanged, used by dashboard)
    /// </summary>
    public event Action<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Raised when reconnecting to the hub
    /// </summary>
    public event Action? Reconnecting;

    /// <summary>
    /// Raised when reconnected to the hub
    /// </summary>
    public event Action<string?>? Reconnected;

    /// <summary>
    /// Current connection state
    /// </summary>
    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Whether currently connected
    /// </summary>
    public bool IsConnected => State == HubConnectionState.Connected;

    /// <summary>
    /// Current connection ID
    /// </summary>
    public string? ConnectionId => _connection?.ConnectionId;

    /// <summary>
    /// The underlying HubConnection (for registering custom handlers)
    /// </summary>
    public HubConnection? Connection => _connection;

    public UserConnectionClient(
        ILogger<UserConnectionClient> logger,
        IOptions<UserManagementOptions> options,
        IServiceProvider serviceProvider,
        IOptions<CommunicatorOptions>? communicatorOptions = null,
        IMethodExecutor? methodExecutor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _communicatorOptions = communicatorOptions?.Value ?? new CommunicatorOptions();
        _methodExecutor = methodExecutor;
    }

    /// <summary>
    /// Starts the connection to the hub with automatic ping/pong support
    /// </summary>
    /// <param name="hubUrl">Hub URL (required)</param>
    /// <param name="accessTokenProvider">Optional function to get the access token (for JWT auth)</param>
    /// <param name="configureOptions">Optional: configure additional HTTP connection options</param>
    public async Task StartAsync(
        string hubUrl,
        Func<Task<string>>? accessTokenProvider = null,
        Action<HttpConnectionOptions>? configureOptions = null)
    {
        if (string.IsNullOrEmpty(hubUrl))
            throw new ArgumentException("Hub URL is required", nameof(hubUrl));

        if (_connection != null)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                _logger.LogWarning("Already connected");
                return;
            }

            // Dispose existing connection before creating a new one
            await _connection.DisposeAsync();
            _connection = null;
        }

        _logger.LogInformation("Starting connection to {Url}", hubUrl);

        var builder = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                if (accessTokenProvider != null)
                {
                    options.AccessTokenProvider = accessTokenProvider;
                }

                configureOptions?.Invoke(options);
            })
            .WithAutomaticReconnect(_options.ReconnectDelaysSeconds.Select(s => TimeSpan.FromSeconds(s)).ToArray());

        _connection = builder.Build();

        // Register internal ping handler for connection validation
        RegisterPingHandler();

        // Register method invocation handler if IMethodExecutor is available
        RegisterMethodInvocationHandler();

        // Register connection lifecycle events
        _connection.Closed += _ =>
        {
            _logger.LogWarning("Connection closed");
            ConnectionChanged?.Invoke(false);
            ConnectionStatusChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        _connection.Reconnecting += _ =>
        {
            _logger.LogInformation("Reconnecting...");
            ConnectionChanged?.Invoke(false);
            ConnectionStatusChanged?.Invoke(false);
            Reconnecting?.Invoke();
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            _logger.LogInformation("Reconnected with {ConnectionId}", connectionId);
            ConnectionChanged?.Invoke(true);
            ConnectionStatusChanged?.Invoke(true);
            Reconnected?.Invoke(connectionId);
            return Task.CompletedTask;
        };

        await ConnectWithRetryAsync();
    }

    /// <summary>
    /// Registers a handler for incoming messages
    /// </summary>
    public void On<T>(string methodName, Action<T> handler)
    {
        EnsureInitialized();
        _connection!.On(methodName, handler);
    }

    /// <summary>
    /// Registers an async handler for incoming messages
    /// </summary>
    public void On<T>(string methodName, Func<T, Task> handler)
    {
        EnsureInitialized();
        _connection!.On(methodName, handler);
    }

    /// <summary>
    /// Sends a message to the hub
    /// </summary>
    public async Task SendAsync(string methodName, object? arg = null)
    {
        EnsureConnected();
        await _connection!.SendAsync(methodName, arg);
    }

    /// <summary>
    /// Invokes a hub method and returns the result
    /// </summary>
    public async Task<T> InvokeAsync<T>(string methodName, object? arg = null)
    {
        EnsureConnected();
        return await _connection!.InvokeAsync<T>(methodName, arg);
    }

    /// <summary>
    /// Stops the connection
    /// </summary>
    public async Task StopAsync()
    {
        if (_connection == null) return;

        _logger.LogInformation("Stopping connection");
        await _connection.StopAsync();
        ConnectionChanged?.Invoke(false);
        ConnectionStatusChanged?.Invoke(false);
    }

    private void RegisterPingHandler()
    {
        // Respond to server pings for connection validation
        _connection!.On("__ping", (string pingId) =>
        {
            _ = RespondToPingAsync(pingId);
        });
    }

    private void RegisterMethodInvocationHandler()
    {
        _logger.LogInformation("RegisterMethodInvocationHandler called, _methodExecutor is {Status}",
            _methodExecutor == null ? "NULL" : "available");

        if (_methodExecutor == null)
        {
            _logger.LogWarning("No IMethodExecutor registered, method invocation handling disabled");
            return;
        }

        _connection!.On<string, string, object>("InvokeMethod", async (requestId, methodName, parameter) =>
        {
            try
            {
                _logger.LogDebug("Received InvokeMethod call: Method={MethodName}, RequestId={RequestId}",
                    methodName, requestId);

                var responseDto = await ExecuteMethodAsync(methodName, parameter);

                _logger.LogDebug("Method {MethodName} executed, sending response: ResponseType={ResponseType}, FilePath={FilePath}, HasJsonData={HasJsonData}, RequestId={RequestId}",
                    methodName, responseDto.ResponseType, responseDto.FilePath, responseDto.JsonData != null, requestId);

                // Serialize for debugging
                var debugJson = JsonSerializer.Serialize(responseDto, _jsonOptions);
                _logger.LogDebug("Sending response JSON: {Json}", debugJson);

                await _connection.InvokeAsync("HandleResponse", requestId, (object?)responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing method {MethodName} for request {RequestId}", methodName, requestId);
                var errorResponse = new SignalRResponse
                {
                    ResponseType = SignalRResponseType.Error,
                    ErrorMessage = ex.Message
                };

                if (_connection != null && _connection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await _connection.InvokeAsync("HandleResponse", requestId, errorResponse);
                    }
                    catch (Exception sendEx)
                    {
                        _logger.LogError(sendEx, "Failed to send error response for request {RequestId}", requestId);
                    }
                }
            }
        });

        _logger.LogDebug("Method invocation handler registered");
    }

    private async Task<SignalRResponse> ExecuteMethodAsync(string methodName, object parameter)
    {
        try
        {
            var result = await _methodExecutor!.ExecuteAsync(methodName, parameter);
            return await PrepareResultForTransmissionAsync(result, methodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing method {MethodName}", methodName);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<SignalRResponse> PrepareResultForTransmissionAsync(object? result, string methodName)
    {
        if (result == null)
        {
            return new SignalRResponse { ResponseType = SignalRResponseType.Null };
        }

        try
        {
            var actualResult = ExtractActionResultValue(result);
            var json = JsonSerializer.Serialize(actualResult, _jsonOptions);
            var dataSize = System.Text.Encoding.UTF8.GetByteCount(json);

            if (dataSize <= _communicatorOptions.MaxDirectDataSizeBytes)
            {
                return new SignalRResponse
                {
                    ResponseType = SignalRResponseType.JsonObject,
                    JsonData = actualResult
                };
            }

            // Data exceeds limit - try file upload if service is available
            // Create a scope to resolve scoped services from a singleton
            using var scope = _serviceProvider.CreateScope();
            var fileUploadService = scope.ServiceProvider.GetService<IFileUploadService>();
            if (fileUploadService != null)
            {
                _logger.LogDebug("Result for method {MethodName} is {Size} bytes, exceeding MaxDirectDataSizeBytes ({Max}). Uploading to file.",
                    methodName, dataSize, _communicatorOptions.MaxDirectDataSizeBytes);

                try
                {
                    var fileName = $"{methodName}_{Guid.NewGuid()}.json";
                    var fileData = System.Text.Encoding.UTF8.GetBytes(json);
                    // Use await instead of .GetAwaiter().GetResult() to avoid blocking on WebAssembly
                    var filePath = await fileUploadService.UploadFileAsync(fileData, fileName, _communicatorOptions.TempFolder);

                    _logger.LogInformation("Large response uploaded to file: {FilePath}", filePath);

                    var response = new SignalRResponse
                    {
                        ResponseType = SignalRResponseType.FilePath,
                        FilePath = filePath
                    };

                    _logger.LogDebug("Returning FilePath response: ResponseType={ResponseType}, FilePath={FilePath}",
                        response.ResponseType, response.FilePath);

                    return response;
                }
                catch (Exception uploadEx)
                {
                    _logger.LogError(uploadEx, "Failed to upload large response for method {MethodName}. Falling back to direct transmission.", methodName);
                    // Fall through to direct transmission
                }
            }
            else
            {
                _logger.LogWarning("Result for method {MethodName} is {Size} bytes, exceeding MaxDirectDataSizeBytes ({Max}). IFileUploadService not registered - sending directly.",
                    methodName, dataSize, _communicatorOptions.MaxDirectDataSizeBytes);
            }

            // Fallback: send directly even though it exceeds the limit
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.JsonObject,
                JsonData = actualResult
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize result for method {MethodName}", methodName);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = $"Failed to serialize result: {ex.Message}"
            };
        }
    }

    private static object? ExtractActionResultValue(object result)
    {
        var actualResult = result;
        if (result.GetType().IsGenericType &&
            result.GetType().GetGenericTypeDefinition().FullName == "Microsoft.AspNetCore.Mvc.ActionResult`1")
        {
            var valueProperty = result.GetType().GetProperty("Value");
            if (valueProperty != null)
            {
                actualResult = valueProperty.GetValue(result);
                if (actualResult == null)
                {
                    var resultProperty = result.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        var objectResult = resultProperty.GetValue(result);
                        if (objectResult != null)
                        {
                            var objectResultValueProp = objectResult.GetType().GetProperty("Value");
                            if (objectResultValueProp != null)
                            {
                                actualResult = objectResultValueProp.GetValue(objectResult);
                            }
                        }
                    }
                }
            }
        }

        return actualResult;
    }

    private async Task RespondToPingAsync(string pingId)
    {
        try
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("Pong", pingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to respond to ping");
        }
    }

    private async Task ConnectWithRetryAsync()
    {
        var delays = _options.ReconnectDelaysSeconds;

        for (var i = 0; i <= delays.Length; i++)
        {
            try
            {
                await _connection!.StartAsync();
                _logger.LogInformation("Connected with {ConnectionId}", _connection.ConnectionId);
                ConnectionChanged?.Invoke(true);
                ConnectionStatusChanged?.Invoke(true);
                return;
            }
            catch (Exception ex)
            {
                if (i < delays.Length)
                {
                    _logger.LogWarning(ex, "Connection attempt {Attempt} failed, retrying in {Delay}s", i + 1, delays[i]);
                    await Task.Delay(TimeSpan.FromSeconds(delays[i]));
                }
                else
                {
                    _logger.LogError(ex, "All connection attempts failed");
                    throw;
                }
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not started. Call StartAsync first.");
    }

    private void EnsureConnected()
    {
        EnsureInitialized();
        if (_connection!.State != HubConnectionState.Connected)
            throw new InvalidOperationException($"Not connected. Current state: {_connection.State}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
