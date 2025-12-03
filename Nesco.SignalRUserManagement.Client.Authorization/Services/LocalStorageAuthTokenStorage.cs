using Microsoft.JSInterop;

namespace Nesco.SignalRUserManagement.Client.Authorization.Services;

/// <summary>
/// Default implementation of <see cref="IAuthTokenStorage"/> using browser localStorage via JSInterop.
/// This is the default storage mechanism for Blazor WebAssembly applications.
/// </summary>
public class LocalStorageAuthTokenStorage : IAuthTokenStorage
{
    private readonly IJSRuntime _jsRuntime;

    private const string TokenKey = "authToken";
    private const string UserIdKey = "userId";
    private const string EmailKey = "userEmail";

    public LocalStorageAuthTokenStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetUserIdAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserIdKey);
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
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", EmailKey);
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
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token ?? "");
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserIdKey, userId);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", EmailKey, email);
        }
        catch
        {
            // If localStorage is not available, continue without persistence
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserIdKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", EmailKey);
        }
        catch
        {
            // If localStorage is not available, continue
        }
    }
}
