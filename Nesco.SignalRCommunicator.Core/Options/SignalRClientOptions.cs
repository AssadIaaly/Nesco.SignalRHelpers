namespace Nesco.SignalRCommunicator.Core.Options;

/// <summary>
/// Configuration options for the SignalR client.
/// </summary>
public class SignalRClientOptions
{
    /// <summary>
    /// Gets or sets the server URL to connect to.
    /// Default: "https://localhost:5004"
    /// </summary>
    public string ServerUrl { get; set; } = "https://localhost:5004";

    /// <summary>
    /// Gets or sets the path to the SignalR hub on the server.
    /// Default: "/hubs/communicator"
    /// </summary>
    public string HubPath { get; set; } = "/hubs/communicator";

    /// <summary>
    /// Gets or sets the keep-alive interval in seconds.
    /// This determines how often the client sends ping messages to keep the connection alive.
    /// Default: 15 seconds
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the server timeout in seconds.
    /// If the client doesn't receive a message from the server within this time, the connection is closed.
    /// Default: 30 seconds
    /// </summary>
    public int ServerTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the delay in seconds before attempting to reconnect after a connection is closed.
    /// Default: 30 seconds
    /// </summary>
    public int ReconnectDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum size in bytes for data to be sent directly through SignalR.
    /// Data larger than this will be uploaded as a file.
    /// Default: 10KB (10 * 1024 bytes)
    /// </summary>
    public int MaxDirectDataSizeBytes { get; set; } = 10 * 1024;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for the initial connection.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in seconds between retry attempts.
    /// Default: 5 seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the folder name for temporary file uploads.
    /// Default: "signalr-temp"
    /// </summary>
    public string TempFolder { get; set; } = "signalr-temp";

    /// <summary>
    /// Gets or sets the base URL for file uploads.
    /// If not specified, defaults to <see cref="ServerUrl"/>.
    /// Use this when the file upload API is hosted on a different server or port.
    /// </summary>
    /// <example>
    /// <code>
    /// options.ServerUrl = "http://localhost:5123";
    /// options.FileUploadBaseUrl = "http://localhost:5124"; // Different server for uploads
    /// </code>
    /// </example>
    public string? FileUploadBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the API route for file uploads.
    /// Default: "api/FileUpload"
    /// </summary>
    public string FileUploadRoute { get; set; } = "api/FileUpload";

    /// <summary>
    /// Gets the effective base URL for file uploads.
    /// Returns <see cref="FileUploadBaseUrl"/> if specified, otherwise <see cref="ServerUrl"/>.
    /// </summary>
    public string EffectiveFileUploadBaseUrl => FileUploadBaseUrl ?? ServerUrl;
}
