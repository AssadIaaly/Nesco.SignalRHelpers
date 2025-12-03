using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Nesco.SignalRUserManagement.Server.Authorization.Providers;

/// <summary>
/// Custom user ID provider for SignalR that extracts user ID from JWT claims
/// </summary>
public class SignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Try to get user ID from different claim types
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? connection.User?.FindFirst(ClaimTypes.Name)?.Value
               ?? connection.User?.FindFirst("sub")?.Value;
    }
}
