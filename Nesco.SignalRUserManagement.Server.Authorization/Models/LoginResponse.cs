namespace Nesco.SignalRUserManagement.Server.Authorization.Models;

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Token { get; set; }
}
