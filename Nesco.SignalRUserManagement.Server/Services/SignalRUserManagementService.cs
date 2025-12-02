using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Models;
using Nesco.SignalRUserManagement.Core.Options;
using Nesco.SignalRUserManagement.Server.Models;
using System.Text.Json;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Default implementation of ISignalRUserManagementService that coordinates
/// user connection management and method invocation functionality.
/// </summary>
public class SignalRUserManagementService<TDbContext> : ISignalRUserManagementService
    where TDbContext : DbContext, IUserConnectionDbContext
{
    private readonly IUserCommunicatorService? _communicator;
    private readonly IUserConnectionService _userManagement;
    private readonly TDbContext _context;
    private readonly IFileReaderService? _fileReaderService;
    private readonly CommunicatorOptions _options;
    private readonly ILogger<SignalRUserManagementService<TDbContext>> _logger;

    public SignalRUserManagementService(
        IUserConnectionService userManagement,
        TDbContext context,
        ILogger<SignalRUserManagementService<TDbContext>> logger,
        IOptions<CommunicatorOptions>? options = null,
        IUserCommunicatorService? communicator = null,
        IFileReaderService? fileReaderService = null)
    {
        _userManagement = userManagement;
        _context = context;
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

        // Get all connection IDs
        var connectedUsers = await GetConnectedUsersAsync();
        var connectionIds = connectedUsers.SelectMany(u => u.Connections.Select(c => c.ConnectionId)).ToList();

        if (connectionIds.Count == 0)
        {
            _logger.LogWarning("No connected users to invoke method on");
            yield break;
        }

        await foreach (var response in _communicator!.InvokeMethodStreamingAsync(connectionIds, methodName, parameter, cancellationToken))
        {
            // Process FilePath responses
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

        // Get all connection IDs for this user
        var connectedUsers = await GetConnectedUsersAsync();
        var userConnections = connectedUsers.FirstOrDefault(u => u.UserId == userId);
        var connectionIds = userConnections?.Connections.Select(c => c.ConnectionId).ToList() ?? new List<string>();

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

        // Get all connection IDs for these users
        var connectedUsers = await GetConnectedUsersAsync();
        var connectionIds = connectedUsers
            .Where(u => userIdList.Contains(u.UserId))
            .SelectMany(u => u.Connections.Select(c => c.ConnectionId))
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

    /// <summary>
    /// Processes a response that may contain a FilePath by reading the file content
    /// and converting it to JsonData.
    /// </summary>
    private async Task<SignalRResponse> ProcessFilePathResponseAsync(SignalRResponse response)
    {
        _logger.LogInformation("=== ProcessFilePathResponse START ===");
        _logger.LogInformation("ResponseType={ResponseType} (int={ResponseTypeInt}), FilePath={FilePath}, HasJsonData={HasJsonData}, ErrorMessage={ErrorMessage}",
            response.ResponseType, (int)response.ResponseType, response.FilePath ?? "(null)", response.JsonData != null, response.ErrorMessage ?? "(null)");

        // Check if response type is FilePath (value 1)
        var isFilePath = response.ResponseType == SignalRResponseType.FilePath;
        _logger.LogInformation("Is FilePath type check: {IsFilePath} (ResponseType enum value: {EnumValue})", isFilePath, response.ResponseType);

        if (!isFilePath)
        {
            _logger.LogInformation("Response is not FilePath type (it is {Type} = {IntValue}), returning as-is", response.ResponseType, (int)response.ResponseType);
            return response;
        }

        if (string.IsNullOrEmpty(response.FilePath))
        {
            _logger.LogWarning("ResponseType is FilePath but FilePath property is null or empty!");
            return response;
        }

        _logger.LogInformation("FilePath response confirmed! Will read file: {FilePath}", response.FilePath);
        _logger.LogInformation("IFileReaderService available: {Available}", _fileReaderService != null);

        if (_fileReaderService == null)
        {
            _logger.LogError("IFileReaderService is NULL! Cannot read file. Make sure EnableCommunicator is true in options.");
            return response;
        }

        _logger.LogInformation("IFileReaderService type: {ServiceType}", _fileReaderService.GetType().FullName);

        try
        {
            _logger.LogInformation("Calling ReadFileAsync for: {FilePath}", response.FilePath);
            var fileContent = await _fileReaderService.ReadFileAsync(response.FilePath);
            _logger.LogInformation("File read successfully, content length: {Length} chars", fileContent?.Length ?? 0);

            if (string.IsNullOrEmpty(fileContent))
            {
                _logger.LogWarning("File content is empty for FilePath: {FilePath}", response.FilePath);
                return response;
            }

            _logger.LogInformation("Parsing JSON content from file (first 500 chars): {Preview}",
                fileContent.Length > 500 ? fileContent.Substring(0, 500) + "..." : fileContent);
            var jsonData = JsonSerializer.Deserialize<JsonElement>(fileContent);
            _logger.LogInformation("JSON parsed successfully, ValueKind: {ValueKind}", jsonData.ValueKind);

            // Auto-delete temp file if enabled
            if (_options.AutoDeleteTempFiles)
            {
                _logger.LogInformation("AutoDeleteTempFiles is enabled, scheduling deletion...");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _fileReaderService.DeleteFileAsync(response.FilePath);
                        _logger.LogInformation("Deleted temp file: {FilePath}", response.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", response.FilePath);
                    }
                });
            }

            _logger.LogInformation("=== ProcessFilePathResponse SUCCESS - Returning JsonObject response ===");

            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.JsonObject,
                JsonData = jsonData
            };
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "FILE NOT FOUND: {FilePath}", response.FilePath);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = $"File not found: {response.FilePath}"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON PARSE ERROR from file: {FilePath}", response.FilePath);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = $"Failed to parse JSON from file: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UNEXPECTED ERROR reading file: {FilePath}", response.FilePath);
            return new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = $"Error reading file: {ex.Message}"
            };
        }
    }

    public int GetConnectedUsersCount()
    {
        return _userManagement.GetConnectedUsersCount();
    }

    public bool IsUserConnected(string userId)
    {
        return _userManagement.IsUserConnected(userId);
    }

    public async Task<List<ConnectedUserInfo>> GetConnectedUsersAsync()
    {
        _logger.LogInformation("Fetching connected users...");

        // Purge stale connections first
        await _userManagement.PurgeStaleConnectionsAsync();

        // Get connected users from database - all connections in the table are active
        var connectedUsers = await _context.UserConnections
            .GroupBy(uc => uc.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Connections = g.ToList()
            })
            .ToListAsync();

        _logger.LogInformation("Found {Count} connected users", connectedUsers.Count);

        var result = new List<ConnectedUserInfo>();

        foreach (var cu in connectedUsers)
        {
            var lastConnection = cu.Connections.OrderByDescending(c => c.ConnectedAt).FirstOrDefault();
            // Get username from the most recent connection (they should all have the same username for a user)
            var userName = lastConnection?.Username;

            result.Add(new ConnectedUserInfo
            {
                UserId = cu.UserId,
                Username = userName,
                LastConnect = lastConnection?.ConnectedAt,
                Connections = cu.Connections.Select(c => new UserConnectionInfo
                {
                    ConnectionId = c.ConnectionId,
                    ConnectedAt = c.ConnectedAt
                }).ToList()
            });
        }

        _logger.LogInformation("Returning {Count} users", result.Count);
        return result;
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
