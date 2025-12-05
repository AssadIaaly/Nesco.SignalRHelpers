namespace Nesco.SignalRUserManagement.Client.Authorization.Models;

public class LoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Response from the check-connection endpoint indicating if user is already connected elsewhere.
/// </summary>
public class CheckConnectionResult
{
    public bool IsConnected { get; set; }
    public int ConnectionCount { get; set; }
}
