using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Models;
using Nesco.SignalRUserManagement.Server.Services;
using System.Text.Json;

namespace Nesco.SignalRUserManagement.Server.Hubs;

/// <summary>
/// SignalR hub that manages user connections in memory.
/// No database dependency - connections are tracked by InMemoryConnectionTracker.
/// </summary>
[Authorize]
public class UserManagementHub : Hub
{
    private readonly InMemoryConnectionTracker _tracker;
    private readonly ILogger<UserManagementHub> _logger;
    private readonly IResponseManager? _responseManager;

    public UserManagementHub(
        InMemoryConnectionTracker tracker,
        ILogger<UserManagementHub> logger,
        IResponseManager? responseManager = null)
    {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _responseManager = responseManager;
    }

    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Connection {ConnectionId} has no user identifier", connectionId);
            return base.OnConnectedAsync();
        }

        // Get username from claims
        var username = GetUsernameFromClaims();

        _tracker.AddConnection(connectionId, userId, username);
        _logger.LogInformation("User {UserId} ({Username}) connected with {ConnectionId}",
            userId, username ?? "(no username)", connectionId);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;

        if (!string.IsNullOrEmpty(userId))
        {
            _tracker.RemoveConnection(connectionId);
            _logger.LogInformation("User {UserId} disconnected from {ConnectionId}", userId, connectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called by clients to send response back for a method invocation request.
    /// </summary>
    public Task HandleResponse(string requestId, object? result)
    {
        if (_responseManager == null)
        {
            _logger.LogWarning("HandleResponse called but IResponseManager is not registered. Enable communicator functionality to use method invocation.");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogDebug("HandleResponse for RequestId: {RequestId}", requestId);

            SignalRResponse? responseDto = null;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            if (result is JsonElement jsonElement)
            {
                var rawJson = jsonElement.GetRawText();
                responseDto = JsonSerializer.Deserialize<SignalRResponse>(rawJson, jsonOptions);
            }
            else if (result is SignalRResponse directDto)
            {
                responseDto = directDto;
            }
            else if (result != null)
            {
                var json = JsonSerializer.Serialize(result, jsonOptions);
                responseDto = JsonSerializer.Deserialize<SignalRResponse>(json, jsonOptions);
            }

            responseDto ??= new SignalRResponse { ResponseType = SignalRResponseType.Null };

            // Check if this is a streaming multi-response request (format: "{baseRequestId}:{connectionId}")
            bool completed;
            if (requestId.Contains(':'))
            {
                var parts = requestId.Split(':', 2);
                var baseRequestId = parts[0];
                var connectionIdPart = parts[1];
                completed = _responseManager.AddStreamingResponse(baseRequestId, connectionIdPart, responseDto);
            }
            else
            {
                completed = _responseManager.CompleteRequest(requestId, responseDto);
            }

            if (!completed)
            {
                _logger.LogWarning("Received response for unknown request ID: {RequestId}", requestId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client response for request {RequestId}", requestId);

            var errorResponse = new SignalRResponse
            {
                ResponseType = SignalRResponseType.Error,
                ErrorMessage = ex.Message
            };

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

    private string? GetUsernameFromClaims()
    {
        // Try JWT/Bearer identity first
        var jwtIdentity = Context.User?.Identities?.FirstOrDefault(i =>
            i.IsAuthenticated &&
            (i.AuthenticationType == "Bearer" ||
             i.AuthenticationType == "AuthenticationTypes.Federation"));

        if (jwtIdentity != null)
        {
            var username = jwtIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? jwtIdentity.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? jwtIdentity.FindFirst("name")?.Value
                ?? jwtIdentity.FindFirst("email")?.Value;

            if (!string.IsNullOrEmpty(username))
                return username;
        }

        // Fall back to default identity claims
        return Context.User?.Identity?.Name
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? Context.User?.FindFirst("name")?.Value
            ?? Context.User?.FindFirst("email")?.Value
            ?? Context.User?.FindFirst("preferred_username")?.Value;
    }
}
