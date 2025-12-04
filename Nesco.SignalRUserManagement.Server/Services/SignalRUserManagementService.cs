using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Models;
using Nesco.SignalRUserManagement.Core.Options;
using System.Text.Json;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Default implementation of ISignalRUserManagementService that coordinates
/// user connection management and method invocation functionality.
/// Uses in-memory connection tracking - no database required.
/// </summary>
public class SignalRUserManagementService : ISignalRUserManagementService
{
    private readonly IUserCommunicatorService? _communicator;
    private readonly IUserConnectionService _userManagement;
    private readonly InMemoryConnectionTracker _tracker;
    private readonly IFileReaderService? _fileReaderService;
    private readonly CommunicatorOptions _options;
    private readonly ILogger<SignalRUserManagementService> _logger;

    public SignalRUserManagementService(
        IUserConnectionService userManagement,
        InMemoryConnectionTracker tracker,
        ILogger<SignalRUserManagementService> logger,
        IOptions<CommunicatorOptions>? options = null,
        IUserCommunicatorService? communicator = null,
        IFileReaderService? fileReaderService = null)
    {
        _userManagement = userManagement;
        _tracker = tracker;
        _logger = logger;
        _options = options?.Value ?? new CommunicatorOptions();
        _communicator = communicator;
        _fileReaderService = fileReaderService;
    }

    public async Task<SignalRResponse> InvokeOnAllConnectedAsync(string methodName, object? parameter)
    {
        EnsureCommunicatorEnabled();
        _logger.LogInformation("Invoking {Method} on all connected users", methodName);
        var response = await _communicator!.InvokeMethodAsync(methodName, parameter);
        return await ProcessFilePathResponseAsync(response);
    }

    public async Task<SignalRResponse> InvokeOnUserAsync(string userId, string methodName, object? parameter)
    {
        EnsureCommunicatorEnabled();
        _logger.LogInformation("Invoking {Method} on user {UserId}", methodName, userId);

        if (!_userManagement.IsUserConnected(userId))
        {
            throw new InvalidOperationException($"User {userId} is not connected");
        }

        var response = await _communicator!.InvokeMethodOnUserAsync(userId, methodName, parameter);
        return await ProcessFilePathResponseAsync(response);
    }

    public async Task<SignalRResponse> InvokeOnUsersAsync(IEnumerable<string> userIds, string methodName, object? parameter)
    {
        EnsureCommunicatorEnabled();
        var userIdList = userIds.ToList();
        _logger.LogInformation("Invoking {Method} on {Count} users", methodName, userIdList.Count);

        var response = await _communicator!.InvokeMethodOnUsersAsync(userIdList, methodName, parameter);
        return await ProcessFilePathResponseAsync(response);
    }

    public async Task<SignalRResponse> InvokeOnConnectionAsync(string connectionId, string methodName, object? parameter)
    {
        EnsureCommunicatorEnabled();
        _logger.LogInformation("Invoking {Method} on connection {ConnectionId}", methodName, connectionId);
        var response = await _communicator!.InvokeMethodOnConnectionAsync(connectionId, methodName, parameter);
        return await ProcessFilePathResponseAsync(response);
    }

    // ===== Streaming Multi-Response Method Implementations =====

    public async IAsyncEnumerable<ClientResponse> InvokeOnAllConnectedStreamingAsync(
        string methodName,
        object? parameter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureCommunicatorEnabled();
        _logger.LogInformation("Invoking {Method} on all connected users (streaming)", methodName);

        var connectionIds = _tracker.GetAllConnectionIds();

        if (connectionIds.Count == 0)
        {
            _logger.LogWarning("No connected users to invoke method on");
            yield break;
        }

        await foreach (var response in _communicator!.InvokeMethodStreamingAsync(connectionIds, methodName, parameter, cancellationToken))
        {
            if (response.Response != null)
            {
                response.Response = await ProcessFilePathResponseAsync(response.Response);
            }
            yield return response;
        }
    }

    public async IAsyncEnumerable<ClientResponse> InvokeOnUserStreamingAsync(
        string userId,
        string methodName,
        object? parameter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureCommunicatorEnabled();
        _logger.LogInformation("Invoking {Method} on user {UserId} (streaming)", methodName, userId);

        if (!_userManagement.IsUserConnected(userId))
        {
            throw new InvalidOperationException($"User {userId} is not connected");
        }

        var connectionIds = _tracker.GetUserConnections(userId);

        if (connectionIds.Count == 0)
        {
            yield break;
        }

        await foreach (var response in _communicator!.InvokeMethodStreamingAsync(connectionIds, methodName, parameter, cancellationToken))
        {
            if (response.Response != null)
            {
                response.Response = await ProcessFilePathResponseAsync(response.Response);
            }
            yield return response;
        }
    }

    public async IAsyncEnumerable<ClientResponse> InvokeOnUsersStreamingAsync(
        IEnumerable<string> userIds,
        string methodName,
        object? parameter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureCommunicatorEnabled();
        var userIdList = userIds.ToList();
        _logger.LogInformation("Invoking {Method} on {Count} users (streaming)", methodName, userIdList.Count);

        var connectionIds = userIdList
            .SelectMany(userId => _tracker.GetUserConnections(userId))
            .ToList();

        if (connectionIds.Count == 0)
        {
            yield break;
        }

        await foreach (var response in _communicator!.InvokeMethodStreamingAsync(connectionIds, methodName, parameter, cancellationToken))
        {
            if (response.Response != null)
            {
                response.Response = await ProcessFilePathResponseAsync(response.Response);
            }
            yield return response;
        }
    }

    public async IAsyncEnumerable<ClientResponse> InvokeOnConnectionsStreamingAsync(
        IEnumerable<string> connectionIds,
        string methodName,
        object? parameter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureCommunicatorEnabled();
        var connectionIdList = connectionIds.ToList();
        _logger.LogInformation("Invoking {Method} on {Count} connections (streaming)", methodName, connectionIdList.Count);

        await foreach (var response in _communicator!.InvokeMethodStreamingAsync(connectionIdList, methodName, parameter, cancellationToken))
        {
            if (response.Response != null)
            {
                response.Response = await ProcessFilePathResponseAsync(response.Response);
            }
            yield return response;
        }
    }

    private async Task<SignalRResponse> ProcessFilePathResponseAsync(SignalRResponse response)
    {
        if (response.ResponseType != SignalRResponseType.FilePath || string.IsNullOrEmpty(response.FilePath))
        {
            return response;
        }

        if (_fileReaderService == null)
        {
            _logger.LogError("IFileReaderService is not available. Cannot read file response.");
            return response;
        }

        try
        {
            var fileContent = await _fileReaderService.ReadFileAsync(response.FilePath);

            if (string.IsNullOrEmpty(fileContent))
            {
                _logger.LogWarning("File content is empty for FilePath: {FilePath}", response.FilePath);
                return response;
            }

            var jsonData = JsonSerializer.Deserialize<JsonElement>(fileContent);

            // Auto-delete temp file if enabled
            if (_options.AutoDeleteTempFiles)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _fileReaderService.DeleteFileAsync(response.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", response.FilePath);
                    }
                });
            }

            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.JsonObject,
                JsonData = jsonData
            };
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found: {FilePath}", response.FilePath);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = $"File not found: {response.FilePath}"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parse error from file: {FilePath}", response.FilePath);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = $"Failed to parse JSON from file: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", response.FilePath);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = $"Error reading file: {ex.Message}"
            };
        }
    }

    public int GetConnectedUsersCount()
    {
        return _tracker.GetConnectedUsersCount();
    }

    public bool IsUserConnected(string userId)
    {
        return _tracker.IsUserConnected(userId);
    }

    public Task<List<ConnectedUserInfo>> GetConnectedUsersAsync()
    {
        var groups = _tracker.GetAllConnections();

        var result = groups.Select(g => new ConnectedUserInfo
        {
            UserId = g.UserId,
            Username = g.Username,
            LastConnect = g.Connections.OrderByDescending(c => c.ConnectedAt).FirstOrDefault()?.ConnectedAt,
            Connections = g.Connections.Select(c => new UserConnectionInfo
            {
                ConnectionId = c.ConnectionId,
                ConnectedAt = c.ConnectedAt
            }).ToList()
        }).ToList();

        return Task.FromResult(result);
    }

    private void EnsureCommunicatorEnabled()
    {
        if (_communicator == null)
        {
            throw new InvalidOperationException(
                "Communicator functionality is not enabled. " +
                "Set EnableCommunicator = true in SignalRUserManagementOptions.");
        }
    }
}
