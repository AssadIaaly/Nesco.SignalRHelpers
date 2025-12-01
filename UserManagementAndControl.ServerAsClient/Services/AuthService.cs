using System.Net.Http.Json;
using System.Security.Claims;

namespace UserManagementAndControl.ServerAsClient.Services;

/// <summary>
/// Handles authentication for the Blazor Server client.
/// Registered as singleton to share authentication state across the application.
/// </summary>
public class AuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;

    private ClaimsPrincipal? _currentUser;
    private string? _accessToken;

    public AuthService(IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("ServerApi");

    public bool IsAuthenticated => _currentUser?.Identity?.IsAuthenticated ?? false;
    public ClaimsPrincipal CurrentUser => _currentUser ?? new ClaimsPrincipal();
    public string? AccessToken => _accessToken;

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Attempting login for {Email}", email);

            var httpClient = CreateClient();
            var response = await httpClient.PostAsJsonAsync("api/auth/login", new
            {
                Email = email,
                Password = password
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    _accessToken = result.Token;

                    // Create claims from the response
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, result.UserId),
                        new(ClaimTypes.Email, result.Email),
                        new(ClaimTypes.Name, result.Email)
                    };

                    var identity = new ClaimsIdentity(claims, "jwt");
                    _currentUser = new ClaimsPrincipal(identity);

                    _logger.LogInformation("Login successful for {Email}, UserId: {UserId}", email, result.UserId);
                    return true;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed for {Email}: {StatusCode} - {Error}", email, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", email);
        }

        return false;
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        _accessToken = null;
        _logger.LogInformation("User logged out");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Matches the response from AuthController.Login
/// </summary>
public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Token { get; set; }
}
