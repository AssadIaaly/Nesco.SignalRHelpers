using System.Net.Http.Json;
using System.Security.Claims;
using Nesco.SignalRUserManagement.Client.Authorization.Models;

namespace Nesco.SignalRUserManagement.Client.Authorization.Services;

/// <summary>
/// Service for handling authentication operations.
/// Uses <see cref="IAuthTokenStorage"/> for persisting auth data, allowing different storage mechanisms
/// (localStorage for Blazor WASM, Preferences for MAUI, etc.).
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthTokenStorage _tokenStorage;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private string? _accessToken;
    private bool _initialized;

    public event Action? AuthStateChanged;

    public ClaimsPrincipal CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser.Identity?.IsAuthenticated ?? false;
    public string? AccessToken => _accessToken;
    public string? UserId => _currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public AuthService(HttpClient httpClient, IAuthTokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _accessToken = result.Token;
                    await SetUserAsync(result.UserId, result.Email);

                    // Persist to storage
                    await _tokenStorage.SaveAsync(result.Token, result.UserId, result.Email);

                    return new LoginResult { Success = true };
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            return new LoginResult { Success = false, Error = error };
        }
        catch (Exception ex)
        {
            return new LoginResult { Success = false, Error = ex.Message };
        }
    }

    public async Task LogoutAsync()
    {
        _accessToken = null;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Clear from storage
        await _tokenStorage.ClearAsync();

        AuthStateChanged?.Invoke();
    }

    public async Task<bool> CheckAuthAsync()
    {
        // Initialize from storage on first check
        if (!_initialized)
        {
            await InitializeFromStorageAsync();
            _initialized = true;
        }

        // For JWT, we don't check with server - token is stored client-side
        // If we have a token and user, we're authenticated
        return IsAuthenticated;
    }

    private async Task InitializeFromStorageAsync()
    {
        try
        {
            var token = await _tokenStorage.GetTokenAsync();
            var userId = await _tokenStorage.GetUserIdAsync();
            var email = await _tokenStorage.GetEmailAsync();

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
            {
                _accessToken = token;
                await SetUserAsync(userId, email);
            }
        }
        catch
        {
            // If storage is not available or throws an error, continue without persisted auth
        }
    }

    private Task SetUserAsync(string userId, string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email)
        };

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        AuthStateChanged?.Invoke();
        return Task.CompletedTask;
    }
}
