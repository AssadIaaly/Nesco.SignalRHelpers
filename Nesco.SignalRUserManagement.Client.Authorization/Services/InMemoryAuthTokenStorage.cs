namespace Nesco.SignalRUserManagement.Client.Authorization.Services;

/// <summary>
/// In-memory implementation of <see cref="IAuthTokenStorage"/>.
/// Stores authentication data in memory - suitable for Blazor Server apps
/// where persistence across restarts is not required.
/// Register as Singleton to share state across the application.
/// </summary>
public class InMemoryAuthTokenStorage : IAuthTokenStorage
{
    private string? _token;
    private string? _userId;
    private string? _email;

    public Task<string?> GetTokenAsync() => Task.FromResult(_token);

    public Task<string?> GetUserIdAsync() => Task.FromResult(_userId);

    public Task<string?> GetEmailAsync() => Task.FromResult(_email);

    public Task SaveAsync(string? token, string userId, string email)
    {
        _token = token;
        _userId = userId;
        _email = email;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _token = null;
        _userId = null;
        _email = null;
        return Task.CompletedTask;
    }
}
