using Nesco.SignalRUserManagement.Client.Authorization.Services;

namespace UserManagementAndControl.MAUI.Services;

/// <summary>
/// MAUI implementation of <see cref="IAuthTokenStorage"/> using Preferences.
/// Stores authentication data in platform-specific preferences storage.
/// </summary>
public class PreferencesAuthTokenStorage : IAuthTokenStorage
{
    private const string TokenKey = "authToken";
    private const string UserIdKey = "userId";
    private const string EmailKey = "userEmail";

    public Task<string?> GetTokenAsync()
    {
        var token = Preferences.Default.Get<string?>(TokenKey, null);
        return Task.FromResult(token);
    }

    public Task<string?> GetUserIdAsync()
    {
        var userId = Preferences.Default.Get<string?>(UserIdKey, null);
        return Task.FromResult(userId);
    }

    public Task<string?> GetEmailAsync()
    {
        var email = Preferences.Default.Get<string?>(EmailKey, null);
        return Task.FromResult(email);
    }

    public Task SaveAsync(string? token, string userId, string email)
    {
        Preferences.Default.Set(TokenKey, token ?? "");
        Preferences.Default.Set(UserIdKey, userId);
        Preferences.Default.Set(EmailKey, email);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Default.Remove(TokenKey);
        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(EmailKey);
        return Task.CompletedTask;
    }
}
