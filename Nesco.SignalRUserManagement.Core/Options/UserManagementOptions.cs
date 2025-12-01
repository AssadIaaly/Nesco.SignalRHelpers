namespace Nesco.SignalRUserManagement.Core.Options;

/// <summary>
/// Configuration options for SignalR User Management
/// </summary>
public class UserManagementOptions
{
    /// <summary>
    /// Hub URL for client connection
    /// </summary>
    public string HubUrl { get; set; } = string.Empty;

    /// <summary>
    /// Keep-alive interval in seconds
    /// Default: 15
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Client timeout in seconds
    /// Default: 30
    /// </summary>
    public int ClientTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry delays in seconds for automatic reconnection
    /// Default: [0, 2, 5, 10, 30]
    /// </summary>
    public int[] ReconnectDelaysSeconds { get; set; } = [0, 2, 5, 10, 30];
}
