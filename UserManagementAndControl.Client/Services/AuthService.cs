using System.Net.Http.Json;
using System.Security.Claims;

namespace UserManagementAndControl.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private string? _accessToken;

    public event Action? AuthStateChanged;

    public ClaimsPrincipal CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser.Identity?.IsAuthenticated ?? false;
    public string? AccessToken => _accessToken;
    public string? UserId => _currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
                    SetUser(result.UserId, result.Email);
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

    public Task LogoutAsync()
    {
        _accessToken = null;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        AuthStateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task<bool> CheckAuthAsync()
    {
        // For JWT, we don't check with server - token is stored client-side
        // If we have a token and user, we're authenticated
        return Task.FromResult(IsAuthenticated);
    }

    private void SetUser(string userId, string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email)
        };

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        AuthStateChanged?.Invoke();
    }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Token { get; set; }
}
