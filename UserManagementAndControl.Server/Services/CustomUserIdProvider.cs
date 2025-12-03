using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace UserManagementAndControl.Server.Services;

/// <summary>
/// Custom user ID provider for SignalR that extracts user ID from JWT claims.
/// When multiple auth schemes are active (JWT + Cookie), this provider
/// prioritizes the JWT identity to ensure external clients are identified correctly.
/// </summary>
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // When multiple auth schemes are used (JWT + Cookie), we need to find the correct identity.
        // JWT/Bearer clients should use their JWT claims, not any ambient cookie claims.

        // First, try to get user ID from JWT/Bearer identity (for external clients)
        var jwtIdentity = connection.User?.Identities?.FirstOrDefault(i =>
            i.IsAuthenticated &&
            (i.AuthenticationType == "Bearer" ||
             i.AuthenticationType == "AuthenticationTypes.Federation"));

        if (jwtIdentity != null)
        {
            var userId = jwtIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? jwtIdentity.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                return userId;
            }
        }

        // Fall back to searching all identities (for cookie auth or other schemes)
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? connection.User?.FindFirst("sub")?.Value;
    }
}
