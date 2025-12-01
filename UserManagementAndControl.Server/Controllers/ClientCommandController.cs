using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nesco.SignalRUserManagement.Core.Interfaces;

namespace UserManagementAndControl.Server.Controllers;

/// <summary>
/// Controller for invoking methods on connected SignalR clients
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientCommandController : ControllerBase
{
    private readonly IUserCommunicatorService _communicator;
    private readonly IUserConnectionService _connectionService;
    private readonly ILogger<ClientCommandController> _logger;

    public ClientCommandController(
        IUserCommunicatorService communicator,
        IUserConnectionService connectionService,
        ILogger<ClientCommandController> logger)
    {
        _communicator = communicator;
        _connectionService = connectionService;
        _logger = logger;
    }

    /// <summary>
    /// Invoke a method on all connected clients
    /// </summary>
    [HttpPost("invoke-all")]
    public async Task<IActionResult> InvokeOnAll([FromBody] InvokeRequest request)
    {
        try
        {
            _logger.LogInformation("Invoking method {Method} on all clients", request.MethodName);
            var response = await _communicator.InvokeMethodAsync(request.MethodName, request.Parameter);
            return Ok(new { response.ResponseType, response.JsonData, response.ErrorMessage });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(408, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking method on all clients");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Invoke a method on a specific user's connections
    /// </summary>
    [HttpPost("invoke-user/{userId}")]
    public async Task<IActionResult> InvokeOnUser(string userId, [FromBody] InvokeRequest request)
    {
        try
        {
            if (!_connectionService.IsUserConnected(userId))
            {
                return NotFound(new { error = $"User {userId} is not connected" });
            }

            _logger.LogInformation("Invoking method {Method} on user {UserId}", request.MethodName, userId);
            var response = await _communicator.InvokeMethodOnUserAsync(userId, request.MethodName, request.Parameter);
            return Ok(new { response.ResponseType, response.JsonData, response.ErrorMessage });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(408, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking method on user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Invoke a method on a specific connection
    /// </summary>
    [HttpPost("invoke-connection/{connectionId}")]
    public async Task<IActionResult> InvokeOnConnection(string connectionId, [FromBody] InvokeRequest request)
    {
        try
        {
            _logger.LogInformation("Invoking method {Method} on connection {ConnectionId}", request.MethodName, connectionId);
            var response = await _communicator.InvokeMethodOnConnectionAsync(connectionId, request.MethodName, request.Parameter);
            return Ok(new { response.ResponseType, response.JsonData, response.ErrorMessage });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(408, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking method on connection {ConnectionId}", connectionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get connected users count
    /// </summary>
    [HttpGet("connected-count")]
    public IActionResult GetConnectedCount()
    {
        var count = _connectionService.GetConnectedUsersCount();
        return Ok(new { connectedUsers = count });
    }

    /// <summary>
    /// Check if a user is connected
    /// </summary>
    [HttpGet("is-connected/{userId}")]
    public IActionResult IsUserConnected(string userId)
    {
        var isConnected = _connectionService.IsUserConnected(userId);
        return Ok(new { userId, isConnected });
    }

    /// <summary>
    /// Get user's connection IDs
    /// </summary>
    [HttpGet("connections/{userId}")]
    public async Task<IActionResult> GetUserConnections(string userId)
    {
        var connections = await _connectionService.GetUserConnectionsAsync(userId);
        return Ok(new { userId, connectionIds = connections });
    }
}

public class InvokeRequest
{
    public string MethodName { get; set; } = string.Empty;
    public object? Parameter { get; set; }
}
