namespace Nesco.SignalRUserManagement.Core.Options;

/// <summary>
/// Unified configuration options for SignalR User Management with optional Communicator and Dashboard support.
/// </summary>
public class SignalRUserManagementOptions
{
    /// <summary>
    /// Hub URL path for SignalR connection. Default: "/hubs/usermanagement"
    /// </summary>
    public string HubPath { get; set; } = "/hubs/usermanagement";

    /// <summary>
    /// Keep-alive interval in seconds. Default: 15
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Client timeout in seconds. Default: 30
    /// </summary>
    public int ClientTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry delays in seconds for automatic reconnection. Default: [0, 2, 5, 10, 30]
    /// </summary>
    public int[] ReconnectDelaysSeconds { get; set; } = [0, 2, 5, 10, 30];

    /// <summary>
    /// Enable method invocation (communicator) functionality. Default: true
    /// </summary>
    public bool EnableCommunicator { get; set; } = true;

    /// <summary>
    /// Enable dashboard service for viewing connected users and invoking methods. Default: true
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// Maximum concurrent method invocation requests allowed. Default: 10
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Timeout in seconds for waiting on a method invocation response. Default: 30
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout in seconds for acquiring semaphore slot. Default: 30
    /// </summary>
    public int SemaphoreTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum size in bytes for sending data directly via SignalR.
    /// Data larger than this will be uploaded as a file. Default: 32KB
    /// </summary>
    public int MaxDirectDataSizeBytes { get; set; } = 32 * 1024;

    /// <summary>
    /// Whether to automatically delete temporary files after reading. Default: true
    /// </summary>
    public bool AutoDeleteTempFiles { get; set; } = true;

    /// <summary>
    /// The folder where temporary files are stored. Default: "signalr-temp"
    /// </summary>
    public string TempFolder { get; set; } = "signalr-temp";

    /// <summary>
    /// Maximum file size in bytes allowed for upload. Default: 100MB
    /// </summary>
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// The API route for file uploads. Default: "api/FileUpload"
    /// </summary>
    public string FileUploadRoute { get; set; } = "api/FileUpload";
}
