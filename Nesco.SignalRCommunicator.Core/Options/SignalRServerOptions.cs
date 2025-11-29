namespace Nesco.SignalRCommunicator.Core.Options;

/// <summary>
/// Configuration options for the SignalR server.
/// </summary>
public class SignalRServerOptions
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent requests the server can handle.
    /// This uses a semaphore to limit concurrent method invocations.
    /// Default: 10
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Gets or sets the timeout in seconds for waiting on a response from a client.
    /// Default: 300 seconds (5 minutes)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the timeout in seconds for acquiring a semaphore slot.
    /// Default: 5 seconds
    /// </summary>
    public int SemaphoreTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the folder name where temporary files are stored.
    /// Default: "signalr-temp"
    /// </summary>
    public string TempFolder { get; set; } = "signalr-temp";

    /// <summary>
    /// Gets or sets whether to automatically delete temporary files after reading them.
    /// Default: true
    /// </summary>
    public bool AutoDeleteTempFiles { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use the default file upload API controller.
    /// When enabled, a FileUploadController will be registered at the specified route.
    /// Default: false
    /// </summary>
    public bool UseDefaultFileUploadApi { get; set; } = false;

    /// <summary>
    /// Gets or sets the route prefix for the file upload API.
    /// Default: "api/FileUpload"
    /// </summary>
    public string FileUploadApiRoute { get; set; } = "api/FileUpload";

    /// <summary>
    /// Gets or sets the maximum file size in bytes for uploads.
    /// Default: 100MB (104857600 bytes)
    /// </summary>
    public long MaxFileSize { get; set; } = 104857600;

    /// <summary>
    /// Gets or sets whether the file upload API requires authorization.
    /// Default: false (AllowAnonymous)
    /// </summary>
    public bool RequireAuthorization { get; set; } = false;
}
