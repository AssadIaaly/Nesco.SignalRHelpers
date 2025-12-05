using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ChatApplication.Models;

namespace ChatApplication.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private ClaimsPrincipal? _currentUser;
    private string? _accessToken;
    private string? _userId;

    public event Action? AuthStateChanged;

    public string? LastError { get; private set; }

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsAuthenticated => _currentUser?.Identity?.IsAuthenticated ?? false;
    public ClaimsPrincipal CurrentUser => _currentUser ?? new ClaimsPrincipal();
    public string? AccessToken => _accessToken;
    public string? UserId => _userId;
    public string? UserEmail => _currentUser?.FindFirst(ClaimTypes.Email)?.Value;

    public async Task<bool> LoginAsync(string email, string password)
    {
        LastError = null;

        try
        {
            Console.WriteLine($"[AuthService] Attempting login for {email}");
            Console.WriteLine($"[AuthService] HttpClient BaseAddress: {_httpClient.BaseAddress}");
            Console.WriteLine($"[AuthService] Full URL: {_httpClient.BaseAddress}api/auth/login");

            var response = await _httpClient.PostAsJsonAsync("api/auth/login",
                new LoginRequest(email, password));

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[AuthService] Response status: {response.StatusCode}");
            Console.WriteLine($"[AuthService] Response content: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<LoginResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                {
                    _accessToken = result.Token;
                    _userId = result.UserId;

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, result.UserId),
                        new(ClaimTypes.Email, result.Email),
                        new(ClaimTypes.Name, result.Email)
                    };

                    var identity = new ClaimsIdentity(claims, "jwt");
                    _currentUser = new ClaimsPrincipal(identity);

                    Console.WriteLine($"[AuthService] Login successful for user {result.UserId}");
                    AuthStateChanged?.Invoke();
                    return true;
                }
            }
            else
            {
                LastError = responseContent;
                Console.WriteLine($"[AuthService] Login failed: {responseContent}");
            }
        }
        catch (Exception ex)
        {
            LastError = $"Connection error: {ex.Message}";
            Console.WriteLine($"[AuthService] Login exception: {ex}");
        }

        return false;
    }

    /// <summary>
    /// Checks if a user is already connected from another client before logging in.
    /// </summary>
    public async Task<CheckConnectionResult?> CheckConnectionAsync(string email, string password)
    {
        try
        {
            Console.WriteLine($"[AuthService] Checking existing connections for {email}");
            Console.WriteLine($"[AuthService] BaseAddress: {_httpClient.BaseAddress}");

            var response = await _httpClient.PostAsJsonAsync("api/auth/check-connection",
                new LoginRequest(email, password));

            Console.WriteLine($"[AuthService] CheckConnection response: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AuthService] CheckConnection content: {content}");
                return JsonSerializer.Deserialize<CheckConnectionResult>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AuthService] CheckConnection failed: {response.StatusCode}, {errorContent}");
            }
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"[AuthService] CheckConnection HTTP exception: {httpEx.Message}");
            Console.WriteLine($"[AuthService] Inner: {httpEx.InnerException?.Message}");
            Console.WriteLine($"[AuthService] StatusCode: {httpEx.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] CheckConnection exception: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[AuthService] Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }

        return null;
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        _accessToken = null;
        _userId = null;
        LastError = null;

        Console.WriteLine("[AuthService] Logged out");
        AuthStateChanged?.Invoke();

        return Task.CompletedTask;
    }
}
