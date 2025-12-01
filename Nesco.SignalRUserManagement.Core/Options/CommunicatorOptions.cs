namespace Nesco.SignalRUserManagement.Core.Options;

/// <summary>
/// Configuration options for SignalR method invocation (communicator) functionality.
/// </summary>
public class CommunicatorOptions
{
    /// <summary>
    /// Maximum concurrent requests allowed. Default: 10.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Timeout in seconds for waiting on a response. Default: 30.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout in seconds for acquiring semaphore slot. Default: 30.
    /// </summary>
    public int SemaphoreTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum size in bytes for sending data directly via SignalR (client-side).
    /// Data larger than this will be uploaded as a file. Default: 32KB.
    /// </summary>
    public int MaxDirectDataSizeBytes { get; set; } = 32 * 1024;

    /// <summary>
    /// Whether to automatically delete temporary files after reading (server-side). Default: true.
    /// </summary>
    public bool AutoDeleteTempFiles { get; set; } = true;

    /// <summary>
    /// The folder where temporary files are stored. Default: "signalr-temp".
    /// </summary>
    public string TempFolder { get; set; } = "signalr-temp";

    /// <summary>
    /// Maximum file size in bytes allowed for upload. Default: 100MB.
    /// </summary>
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// The API route for file uploads. Default: "api/FileUpload".
    /// </summary>
    public string FileUploadRoute { get; set; } = "api/FileUpload";
}
