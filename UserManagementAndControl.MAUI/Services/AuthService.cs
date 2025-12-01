using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace UserManagementAndControl.MAUI.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    private ClaimsPrincipal? _currentUser;
    private string? _accessToken;
    private string? _lastError;

    public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsAuthenticated => _currentUser?.Identity?.IsAuthenticated ?? false;
    public ClaimsPrincipal CurrentUser => _currentUser ?? new ClaimsPrincipal();
    public string? AccessToken => _accessToken;
    public string? LastError => _lastError;

    public async Task<bool> LoginAsync(string email, string password)
    {
        _lastError = null;

        try
        {
            var baseUrl = _httpClient.BaseAddress?.ToString() ?? "NO BASE ADDRESS";
            _logger.LogInformation("=== LOGIN ATTEMPT ===");
            _logger.LogInformation("Base URL: {BaseUrl}", baseUrl);
            _logger.LogInformation("Email: {Email}", email);

            Console.WriteLine($"=== LOGIN ATTEMPT ===");
            Console.WriteLine($"Base URL: {baseUrl}");
            Console.WriteLine($"Email: {email}");

            var requestUri = "api/auth/login";
            var fullUrl = $"{baseUrl}{requestUri}";
            _logger.LogInformation("Full request URL: {FullUrl}", fullUrl);
            Console.WriteLine($"Full request URL: {fullUrl}");

            var requestBody = new { Email = email, Password = password };
            _logger.LogInformation("Sending POST request...");
            Console.WriteLine("Sending POST request...");

            var response = await _httpClient.PostAsJsonAsync(requestUri, requestBody);

            _logger.LogInformation("Response Status: {StatusCode} ({ReasonPhrase})",
                (int)response.StatusCode, response.ReasonPhrase);
            Console.WriteLine($"Response Status: {(int)response.StatusCode} ({response.ReasonPhrase})");

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response Content: {Content}", responseContent);
            Console.WriteLine($"Response Content: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    _accessToken = result.Token;

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, result.UserId),
                        new(ClaimTypes.Email, result.Email),
                        new(ClaimTypes.Name, result.Email)
                    };

                    var identity = new ClaimsIdentity(claims, "jwt");
                    _currentUser = new ClaimsPrincipal(identity);

                    _logger.LogInformation("=== LOGIN SUCCESS ===");
                    _logger.LogInformation("UserId: {UserId}", result.UserId);
                    Console.WriteLine($"=== LOGIN SUCCESS ===");
                    Console.WriteLine($"UserId: {result.UserId}");
                    return true;
                }
                else
                {
                    _lastError = "Token was empty in response";
                    _logger.LogWarning("Token was empty in response");
                    Console.WriteLine("Token was empty in response");
                }
            }
            else
            {
                _lastError = $"HTTP {(int)response.StatusCode}: {responseContent}";
                _logger.LogWarning("=== LOGIN FAILED ===");
                _logger.LogWarning("Status: {StatusCode}", response.StatusCode);
                _logger.LogWarning("Response: {Content}", responseContent);
                Console.WriteLine($"=== LOGIN FAILED ===");
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseContent}");
            }
        }
        catch (HttpRequestException httpEx)
        {
            _lastError = $"Network error: {httpEx.Message}";
            _logger.LogError(httpEx, "=== HTTP REQUEST EXCEPTION ===");
            Console.WriteLine($"=== HTTP REQUEST EXCEPTION ===");
            Console.WriteLine($"Message: {httpEx.Message}");
            Console.WriteLine($"Inner: {httpEx.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            _lastError = $"Error: {ex.Message}";
            _logger.LogError(ex, "=== LOGIN EXCEPTION ===");
            Console.WriteLine($"=== LOGIN EXCEPTION ===");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
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

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Token { get; set; }
}
