using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Models;
using Nesco.SignalRUserManagement.Core.Options;
using System.Text.Json;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Service for invoking methods on connected SignalR clients and receiving responses.
/// Works with any hub type.
/// </summary>
/// <typeparam name="THub">The hub type to use for communication.</typeparam>
public class UserCommunicatorService<THub> : IUserCommunicatorService
    where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;
    private readonly ILogger<UserCommunicatorService<THub>> _logger;
    private readonly CommunicatorOptions _options;
    private readonly IResponseManager _responseManager;
    private readonly IFileReaderService? _fileReaderService;
    private readonly SemaphoreSlim _requestSemaphore;

    public UserCommunicatorService(
        IHubContext<THub> hubContext,
        ILogger<UserCommunicatorService<THub>> logger,
        IOptions<CommunicatorOptions> options,
        IResponseManager responseManager,
        IFileReaderService? fileReaderService = null)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new CommunicatorOptions();
        _responseManager = responseManager ?? throw new ArgumentNullException(nameof(responseManager));
        _fileReaderService = fileReaderService;
        _requestSemaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
    }

    /// <inheritdoc/>
    public async Task<SignalRResponse> InvokeMethodAsync(string methodName, object? parameter)
    {
        return await InvokeMethodInternalAsync(
            async (requestId) =>
            {
                await _hubContext.Clients.All.SendAsync("InvokeMethod", requestId, methodName, parameter);
            },
            methodName,
            "all clients");
    }

    /// <inheritdoc/>
    public async Task<T?> InvokeMethodAsync<T>(string methodName, object? parameter) where T : class
    {
        var response = await InvokeMethodAsync(methodName, parameter);
        return DecodeResponse<T>(response);
    }

    /// <inheritdoc/>
    public async Task<SignalRResponse> InvokeMethodOnConnectionAsync(string connectionId, string methodName, object? parameter)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("Connection ID cannot be null or empty", nameof(connectionId));

        return await InvokeMethodInternalAsync(
            async (requestId) =>
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("InvokeMethod", requestId, methodName, parameter);
            },
            methodName,
            $"connection {connectionId}");
    }

    /// <inheritdoc/>
    public async Task<T?> InvokeMethodOnConnectionAsync<T>(string connectionId, string methodName, object? parameter) where T : class
    {
        var response = await InvokeMethodOnConnectionAsync(connectionId, methodName, parameter);
        return DecodeResponse<T>(response);
    }

    /// <inheritdoc/>
    public async Task<SignalRResponse> InvokeMethodOnUserAsync(string userId, string methodName, object? parameter)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        return await InvokeMethodInternalAsync(
            async (requestId) =>
            {
                await _hubContext.Clients.User(userId).SendAsync("InvokeMethod", requestId, methodName, parameter);
            },
            methodName,
            $"user {userId}");
    }

    /// <inheritdoc/>
    public async Task<T?> InvokeMethodOnUserAsync<T>(string userId, string methodName, object? parameter) where T : class
    {
        var response = await InvokeMethodOnUserAsync(userId, methodName, parameter);
        return DecodeResponse<T>(response);
    }

    /// <inheritdoc/>
    public async Task<SignalRResponse> InvokeMethodOnUsersAsync(IEnumerable<string> userIds, string methodName, object? parameter)
    {
        if (userIds == null || !userIds.Any())
            throw new ArgumentException("User IDs cannot be null or empty", nameof(userIds));

        var userIdList = userIds.ToList();

        return await InvokeMethodInternalAsync(
            async (requestId) =>
            {
                await _hubContext.Clients.Users(userIdList).SendAsync("InvokeMethod", requestId, methodName, parameter);
            },
            methodName,
            $"users ({string.Join(", ", userIdList.Take(3))}{(userIdList.Count > 3 ? "..." : "")})");
    }

    /// <inheritdoc/>
    public async Task<T?> InvokeMethodOnUsersAsync<T>(IEnumerable<string> userIds, string methodName, object? parameter) where T : class
    {
        var response = await InvokeMethodOnUsersAsync(userIds, methodName, parameter);
        return DecodeResponse<T>(response);
    }

    /// <inheritdoc/>
    public async Task<SignalRResponse> InvokeMethodOnConnectionsAsync(IEnumerable<string> connectionIds, string methodName, object? parameter)
    {
        if (connectionIds == null || !connectionIds.Any())
            throw new ArgumentException("Connection IDs cannot be null or empty", nameof(connectionIds));

        var connectionIdList = connectionIds.ToList();

        return await InvokeMethodInternalAsync(
            async (requestId) =>
            {
                await _hubContext.Clients.Clients(connectionIdList).SendAsync("InvokeMethod", requestId, methodName, parameter);
            },
            methodName,
            $"connections ({string.Join(", ", connectionIdList.Take(3))}{(connectionIdList.Count > 3 ? "..." : "")})");
    }

    /// <inheritdoc/>
    public async Task<T?> InvokeMethodOnConnectionsAsync<T>(IEnumerable<string> connectionIds, string methodName, object? parameter) where T : class
    {
        var response = await InvokeMethodOnConnectionsAsync(connectionIds, methodName, parameter);
        return DecodeResponse<T>(response);
    }

    // ===== Streaming Multi-Response Method Implementation =====

    /// <inheritdoc/>
    public async IAsyncEnumerable<ClientResponse> InvokeMethodStreamingAsync(
        IEnumerable<string> connectionIds,
        string methodName,
        object? parameter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var connectionIdList = connectionIds.ToList();

        if (connectionIdList.Count == 0)
        {
            yield break;
        }

        if (!await _requestSemaphore.WaitAsync(TimeSpan.FromSeconds(_options.SemaphoreTimeoutSeconds), cancellationToken))
        {
            throw new InvalidOperationException($"Server has reached maximum concurrent requests ({_options.MaxConcurrentRequests}). Please try again later.");
        }

        var baseRequestId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("Invoking method {MethodName} on {Count} connections (streaming) with BaseRequestId: {RequestId}",
                methodName, connectionIdList.Count, baseRequestId);

            // Register streaming request - get channel reader to yield responses
            var channelReader = _responseManager.RegisterStreamingRequest(baseRequestId, connectionIdList.Count);

            // Send to each connection with a unique request ID
            var sendTasks = connectionIdList.Select(async connectionId =>
            {
                var perConnectionRequestId = $"{baseRequestId}:{connectionId}";
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("InvokeMethod", perConnectionRequestId, methodName, parameter, cancellationToken);
                    _logger.LogDebug("Sent InvokeMethod to connection {ConnectionId} with RequestId {RequestId}",
                        connectionId, perConnectionRequestId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send InvokeMethod to connection {ConnectionId}", connectionId);
                    // Add a failure response for this connection
                    _responseManager.AddStreamingResponse(baseRequestId, connectionId, new SignalRResponse
                    {
                        ResponseType = SignalRResponseType.Error,
                        ErrorMessage = $"Failed to send: {ex.Message}"
                    });
                }
            });

            // Start sending in background - don't wait for all sends to complete before yielding
            _ = Task.WhenAll(sendTasks);

            _logger.LogDebug("Started sending InvokeMethod calls. Streaming responses as they arrive...");

            // Set up timeout to complete the channel if not all responses arrive
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

            // Register timeout handler
            timeoutCts.Token.Register(() =>
            {
                _logger.LogDebug("Timeout or cancellation for streaming request {RequestId}", baseRequestId);
                _responseManager.CompleteStreamingRequest(baseRequestId);
            });

            // Yield responses as they arrive from the channel
            await foreach (var response in channelReader.ReadAllAsync(cancellationToken))
            {
                _logger.LogDebug("Yielding response from connection {ConnectionId}", response.ConnectionId);
                yield return response;
            }

            _logger.LogDebug("Streaming request {RequestId} completed", baseRequestId);
        }
        finally
        {
            _responseManager.RemoveRequest(baseRequestId);
            _requestSemaphore.Release();
        }
    }

    private async Task<SignalRResponse> InvokeMethodInternalAsync(
        Func<string, Task> sendAction,
        string methodName,
        string target)
    {
        if (!await _requestSemaphore.WaitAsync(TimeSpan.FromSeconds(_options.SemaphoreTimeoutSeconds)))
        {
            throw new InvalidOperationException($"Server has reached maximum concurrent requests ({_options.MaxConcurrentRequests}). Please try again later.");
        }

        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<SignalRResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            _logger.LogDebug("Invoking method {MethodName} on {Target} with RequestId: {RequestId}",
                methodName, target, requestId);

            _responseManager.RegisterRequest(requestId, tcs);
            await sendAction(requestId);

            _logger.LogDebug("Method {MethodName} sent to {Target} with RequestId: {RequestId}. Waiting for response...",
                methodName, target, requestId);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));
            cts.Token.Register(() =>
            {
                _responseManager.RemoveRequest(requestId);
                tcs.TrySetCanceled();
            });

            var result = await tcs.Task;
            _logger.LogDebug("Received response for RequestId: {RequestId}", requestId);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request {RequestId} to {MethodName} on {Target} timed out after {Timeout} seconds",
                requestId, methodName, target, _options.RequestTimeoutSeconds);
            throw new TimeoutException($"Request to {methodName} on {target} timed out after {_options.RequestTimeoutSeconds} seconds");
        }
        finally
        {
            _responseManager.RemoveRequest(requestId);
            _requestSemaphore.Release();
        }
    }

    private T? DecodeResponse<T>(SignalRResponse response) where T : class
    {
        if (response == null) return null;

        if (!string.IsNullOrEmpty(response.ErrorMessage))
        {
            _logger.LogError("Client returned error: {ErrorMessage}", response.ErrorMessage);
            return null;
        }

        if (response.ResponseType == SignalRResponseType.Null)
        {
            return null;
        }

        if (response.ResponseType == SignalRResponseType.JsonObject && response.JsonData != null)
        {
            return DeserializeResult<T>(response.JsonData);
        }

        if (response.ResponseType == SignalRResponseType.FilePath && !string.IsNullOrEmpty(response.FilePath))
        {
            return ReadFileAndDeserialize<T>(response.FilePath);
        }

        return null;
    }

    private T? ReadFileAndDeserialize<T>(string filePath) where T : class
    {
        if (_fileReaderService == null)
        {
            _logger.LogWarning("FilePath response received but IFileReaderService is not registered. FilePath: {FilePath}", filePath);
            return null;
        }

        try
        {
            _logger.LogDebug("Reading file from FilePath: {FilePath}", filePath);
            var fileContent = _fileReaderService.ReadFileAsync(filePath).GetAwaiter().GetResult();

            if (string.IsNullOrEmpty(fileContent))
            {
                _logger.LogWarning("File content is empty for FilePath: {FilePath}", filePath);
                return null;
            }

            // Auto-delete temp file if enabled
            if (_options.AutoDeleteTempFiles)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _fileReaderService.DeleteFileAsync(filePath);
                        _logger.LogDebug("Deleted temp file: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", filePath);
                    }
                });
            }

            // Deserialize JSON content from file
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<T>(fileContent, options);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found: {FilePath}", filePath);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize file content from {FilePath}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file from {FilePath}", filePath);
            return null;
        }
    }

    private T? DeserializeResult<T>(object? result) where T : class
    {
        if (result == null) return null;

        if (result is T directCast)
        {
            return directCast;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        if (result is JsonElement jsonElement)
        {
            try
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    var jsonStr = jsonElement.GetString();
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        var parsedJson = JsonDocument.Parse(jsonStr);
                        return JsonSerializer.Deserialize<T>(parsedJson.RootElement.GetRawText(), options);
                    }
                }
                else
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize JsonElement to type {TypeName}", typeof(T).Name);
                return null;
            }
        }

        if (result is string jsonString)
        {
            try
            {
                var doc = JsonDocument.Parse(jsonString);
                return JsonSerializer.Deserialize<T>(doc.RootElement.GetRawText(), options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize string result to type {TypeName}", typeof(T).Name);
                return null;
            }
        }

        try
        {
            var json = JsonSerializer.Serialize(result);
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert result to type {TypeName}", typeof(T).Name);
            return null;
        }
    }
}
