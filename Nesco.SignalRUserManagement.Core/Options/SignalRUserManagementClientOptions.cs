namespace Nesco.SignalRUserManagement.Core.Options;

/// <summary>
/// Unified configuration options for SignalR User Management client.
/// </summary>
public class SignalRUserManagementClientOptions
{
    /// <summary>
    /// Hub URL for SignalR connection. Can also be provided at runtime via StartAsync.
    /// </summary>
    public string? HubUrl { get; set; }

    /// <summary>
    /// Retry delays in seconds for automatic reconnection. Default: [0, 2, 5, 10, 30]
    /// </summary>
    public int[] ReconnectDelaysSeconds { get; set; } = [0, 2, 5, 10, 30];

    /// <summary>
    /// Maximum size in bytes for sending data directly via SignalR.
    /// Data larger than this will be uploaded as a file if IFileUploadService is registered. Default: 32KB
    /// </summary>
    public int MaxDirectDataSizeBytes { get; set; } = 32 * 1024;

    /// <summary>
    /// The folder where temporary files are stored for upload. Default: "signalr-temp"
    /// </summary>
    public string TempFolder { get; set; } = "signalr-temp";

    /// <summary>
    /// The API route for file uploads. Default: "api/FileUpload"
    /// </summary>
    public string FileUploadRoute { get; set; } = "api/FileUpload";

    /// <summary>
    /// Enable automatic file upload for large responses. Default: true
    /// When enabled and IFileUploadService is registered, large responses will be uploaded as files.
    /// </summary>
    public bool EnableFileUpload { get; set; } = true;
}
