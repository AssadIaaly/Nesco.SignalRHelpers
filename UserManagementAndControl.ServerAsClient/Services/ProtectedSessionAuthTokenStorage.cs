using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Nesco.SignalRUserManagement.Client.Authorization.Services;

namespace UserManagementAndControl.ServerAsClient.Services;

/// <summary>
/// ProtectedSessionStorage-based implementation of IAuthTokenStorage for Blazor Server.
/// Stores authentication data in encrypted browser session storage.
/// Data persists across page refreshes but is cleared when the browser tab is closed.
/// Register as Scoped to match the Blazor circuit lifetime.
/// </summary>
public class ProtectedSessionAuthTokenStorage : IAuthTokenStorage
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private const string TokenKey = "SignalRAuth.Token";
    private const string UserIdKey = "SignalRAuth.UserId";
    private const string EmailKey = "SignalRAuth.Email";

    public ProtectedSessionAuthTokenStorage(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(TokenKey);
            return result.Success ? result.Value : null;
        }
        catch
        {
            // Session storage not available yet (pre-rendering)
            return null;
        }
    }

    public async Task<string?> GetUserIdAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(UserIdKey);
            return result.Success ? result.Value : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetEmailAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(EmailKey);
            return result.Success ? result.Value : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(string? token, string userId, string email)
    {
        try
        {
            if (!string.IsNullOrEmpty(token))
            {
                await _sessionStorage.SetAsync(TokenKey, token);
                await _sessionStorage.SetAsync(UserIdKey, userId);
                await _sessionStorage.SetAsync(EmailKey, email);
            }
            else
            {
                await ClearAsync();
            }
        }
        catch
        {
            // Ignore errors during pre-rendering
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _sessionStorage.DeleteAsync(TokenKey);
            await _sessionStorage.DeleteAsync(UserIdKey);
            await _sessionStorage.DeleteAsync(EmailKey);
        }
        catch
        {
            // Ignore errors during cleanup or pre-rendering
        }
    }
}
