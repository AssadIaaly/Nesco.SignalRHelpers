using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Server.Models;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Service for sending messages to connected users
/// </summary>
public class UserConnectionService<THub, TDbContext> : IUserConnectionService
    where THub : Hub
    where TDbContext : DbContext
{
    private readonly IHubContext<THub> _hubContext;
    private readonly TDbContext _dbContext;
    private readonly ILogger<UserConnectionService<THub, TDbContext>> _logger;

    // Track active connections that respond to ping
    private static readonly HashSet<string> _activeConnections = new();
    private static readonly object _lock = new();

    public UserConnectionService(
        IHubContext<THub> hubContext,
        TDbContext dbContext,
        ILogger<UserConnectionService<THub, TDbContext>> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendToAllAsync(string method, object? data = null)
    {
        await _hubContext.Clients.All.SendAsync(method, data);
    }

    public async Task SendToUserAsync(string userId, string method, object? data = null)
    {
        var connectionIds = await GetUserConnectionsAsync(userId);
        if (connectionIds.Count == 0)
        {
            _logger.LogDebug("No connections found for user {UserId}", userId);
            return;
        }

        await _hubContext.Clients.Clients(connectionIds).SendAsync(method, data);
    }

    public async Task SendToConnectionAsync(string connectionId, string method, object? data = null)
    {
        await _hubContext.Clients.Client(connectionId).SendAsync(method, data);
    }

    public bool IsUserConnected(string userId)
    {
        return _dbContext.Set<UserConnection>().Any(c => c.UserId == userId);
    }

    public int GetConnectedUsersCount()
    {
        return _dbContext.Set<UserConnection>()
            .Select(c => c.UserId)
            .Distinct()
            .Count();
    }

    public async Task<IReadOnlyList<string>> GetUserConnectionsAsync(string userId)
    {
        return await _dbContext.Set<UserConnection>()
            .Where(c => c.UserId == userId)
            .Select(c => c.ConnectionId)
            .ToListAsync();
    }

    public async Task<int> PurgeStaleConnectionsAsync()
    {
        var dbConnections = await _dbContext.Set<UserConnection>().ToListAsync();

        if (dbConnections.Count == 0)
            return 0;

        // Clear tracking set
        lock (_lock)
        {
            _activeConnections.Clear();
        }

        // Send ping to all connections - active clients will respond
        var pingId = Guid.NewGuid().ToString();
        try
        {
            await _hubContext.Clients.All.SendAsync("__ping", pingId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending ping to connections");
        }

        // Wait a short time for responses
        await Task.Delay(2000);

        // Get connections that responded
        HashSet<string> activeIds;
        lock (_lock)
        {
            activeIds = new HashSet<string>(_activeConnections);
        }

        // Find stale connections (those that didn't respond)
        // If no clients responded at all, don't purge (ping handler might not be set up)
        // Instead, fall back to time-based purge for connections older than 5 minutes
        List<UserConnection> staleConnections;

        if (activeIds.Count > 0)
        {
            // Clients are responding to ping - remove those that didn't respond
            staleConnections = dbConnections
                .Where(c => !activeIds.Contains(c.ConnectionId))
                .ToList();
        }
        else
        {
            // No clients responded - use time-based fallback
            var threshold = DateTime.UtcNow.AddMinutes(-5);
            staleConnections = dbConnections
                .Where(c => c.ConnectedAt < threshold)
                .ToList();
        }

        if (staleConnections.Count > 0)
        {
            _dbContext.Set<UserConnection>().RemoveRange(staleConnections);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Purged {Count} stale connections", staleConnections.Count);
        }

        return staleConnections.Count;
    }

    /// <summary>
    /// Called by the hub when a client responds to a ping
    /// </summary>
    public static void MarkConnectionActive(string connectionId)
    {
        lock (_lock)
        {
            _activeConnections.Add(connectionId);
        }
    }
}
