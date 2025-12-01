using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Models;
using Nesco.SignalRUserManagement.Server.Models;
using Nesco.SignalRUserManagement.Server.Services;
using System.Text.Json;

namespace Nesco.SignalRUserManagement.Server.Hubs;

/// <summary>
/// SignalR hub that manages user connections in the database
/// </summary>
/// <typeparam name="TDbContext">DbContext with UserConnections DbSet</typeparam>
[Authorize]
public class UserManagementHub<TDbContext> : Hub where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<UserManagementHub<TDbContext>> _logger;
    private readonly IResponseManager? _responseManager;

    public UserManagementHub(TDbContext dbContext, ILogger<UserManagementHub<TDbContext>> logger, IResponseManager? responseManager = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _responseManager = responseManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Connection {ConnectionId} has no user identifier", connectionId);
            await base.OnConnectedAsync();
            return;
        }

        _logger.LogInformation("User {UserId} connecting with {ConnectionId}", userId, connectionId);

        try
        {
            // Remove any stale connections for this user (safety cleanup)
            var staleThreshold = DateTime.UtcNow.AddMinutes(-5);
            var staleConnections = await GetDbSet()
                .Where(c => c.UserId == userId && c.ConnectedAt < staleThreshold)
                .ToListAsync();

            if (staleConnections.Count > 0)
            {
                GetDbSet().RemoveRange(staleConnections);
                _logger.LogDebug("Removed {Count} stale connections for user {UserId}", staleConnections.Count, userId);
            }

            // Add the new connection
            GetDbSet().Add(new UserConnection
            {
                ConnectionId = connectionId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("User {UserId} connected with {ConnectionId}", userId, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding connection for user {UserId}", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;

        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} disconnecting from {ConnectionId}", userId, connectionId);

            try
            {
                var connection = await GetDbSet().FindAsync(connectionId);
                if (connection != null)
                {
                    GetDbSet().Remove(connection);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("User {UserId} disconnected from {ConnectionId}", userId, connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing connection {ConnectionId} for user {UserId}", connectionId, userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called by clients to respond to a ping (for connection validation)
    /// </summary>
    public void Pong(string pingId)
    {
        UserConnectionService<UserManagementHub<TDbContext>, TDbContext>
            .MarkConnectionActive(Context.ConnectionId);
    }

    /// <summary>
    /// Called by clients to send response back for a method invocation request.
    /// </summary>
    /// <param name="requestId">The request identifier. Can be in format "{baseRequestId}:{connectionId}" for multi-response requests.</param>
    /// <param name="result">The result object from the client.</param>
    public Task HandleResponse(string requestId, object? result)
    {
        if (_responseManager == null)
        {
            _logger.LogWarning("HandleResponse called but IResponseManager is not registered. Enable communicator functionality to use method invocation.");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("=== HandleResponse START for RequestId: {RequestId} ===", requestId);
            _logger.LogInformation("Result type: {Type}", result?.GetType().FullName ?? "null");

            SignalRResponse? responseDto = null;

            // Use JsonSerializerOptions with string enum converter for robust deserialization
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            if (result is JsonElement jsonElement)
            {
                var rawJson = jsonElement.GetRawText();
                _logger.LogInformation("HandleResponse raw JSON: {RawJson}", rawJson);

                responseDto = JsonSerializer.Deserialize<SignalRResponse>(rawJson, jsonOptions);

                _logger.LogInformation("Deserialized - ResponseType={ResponseType} (int={Int}), FilePath={FilePath}",
                    responseDto?.ResponseType, responseDto != null ? (int)responseDto.ResponseType : -1, responseDto?.FilePath ?? "(null)");
            }
            else if (result is SignalRResponse directDto)
            {
                _logger.LogInformation("Result is already SignalRResponse");
                responseDto = directDto;
            }
            else if (result != null)
            {
                // Try to serialize and deserialize unknown types
                _logger.LogInformation("Result is {Type}, attempting conversion...", result.GetType().FullName);
                var json = JsonSerializer.Serialize(result, jsonOptions);
                _logger.LogInformation("Serialized to: {Json}", json);
                responseDto = JsonSerializer.Deserialize<SignalRResponse>(json, jsonOptions);
            }
            else
            {
                _logger.LogWarning("Result is null");
            }

            if (responseDto == null)
            {
                _logger.LogWarning("responseDto is null after deserialization, creating Null response");
                responseDto = new SignalRResponse
                {
                    ResponseType = SignalRResponseType.Null
                };
            }

            _logger.LogInformation("Final response: ResponseType={ResponseType} (int={Int}), FilePath={FilePath}, HasJsonData={HasJsonData}",
                responseDto.ResponseType, (int)responseDto.ResponseType, responseDto.FilePath ?? "(null)", responseDto.JsonData != null);

            // Check if this is a streaming multi-response request (format: "{baseRequestId}:{connectionId}")
            bool completed;
            if (requestId.Contains(':'))
            {
                var parts = requestId.Split(':', 2);
                var baseRequestId = parts[0];
                var connectionId = parts[1];

                _logger.LogInformation("Streaming response detected: BaseRequestId={BaseRequestId}, ConnectionId={ConnectionId}",
                    baseRequestId, connectionId);

                completed = _responseManager.AddStreamingResponse(baseRequestId, connectionId, responseDto);
                _logger.LogInformation("AddStreamingResponse returned: {Completed}", completed);
            }
            else
            {
                completed = _responseManager.CompleteRequest(requestId, responseDto);
                _logger.LogInformation("CompleteRequest returned: {Completed}", completed);
            }

            if (!completed)
            {
                _logger.LogWarning("Received response for unknown request ID: {RequestId}", requestId);
            }

            _logger.LogInformation("=== HandleResponse END ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client response for request {RequestId}", requestId);

            var errorResponse = new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = ex.Message
            };

            // Handle error for both single and streaming multi-response
            if (requestId.Contains(':'))
            {
                var parts = requestId.Split(':', 2);
                _responseManager.AddStreamingResponse(parts[0], parts[1], errorResponse);
            }
            else
            {
                _responseManager.CompleteRequest(requestId, errorResponse);
            }
        }

        return Task.CompletedTask;
    }

    private DbSet<UserConnection> GetDbSet() => _dbContext.Set<UserConnection>();
}
