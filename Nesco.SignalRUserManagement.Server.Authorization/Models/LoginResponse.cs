namespace Nesco.SignalRUserManagement.Server.Authorization.Models;

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Token { get; set; }

    /// <summary>
    /// Indicates if the user already has an active connection from another client.
    /// When true, the client should ask the user if they want to force logout the other client.
    /// </summary>
    public bool IsAlreadyConnected { get; set; }

    /// <summary>
    /// Number of active connections for this user (0 if not connected elsewhere).
    /// </summary>
    public int ActiveConnectionCount { get; set; }
}
