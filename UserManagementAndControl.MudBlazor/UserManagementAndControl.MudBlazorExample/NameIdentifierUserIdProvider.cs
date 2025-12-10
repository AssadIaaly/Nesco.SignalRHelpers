using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace UserManagementAndControl.MudBlazor;

/// <summary>
/// Custom UserIdProvider that extracts the user ID from the NameIdentifier claim.
/// This is used with ASP.NET Core Identity cookie authentication.
/// </summary>
public class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // ASP.NET Core Identity uses NameIdentifier for the user ID
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
