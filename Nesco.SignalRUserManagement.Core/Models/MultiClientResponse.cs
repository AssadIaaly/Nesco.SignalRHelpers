namespace Nesco.SignalRUserManagement.Core.Models;

/// <summary>
/// Represents the aggregated responses from multiple SignalR clients.
/// </summary>
public class MultiClientResponse
{
    /// <summary>
    /// Gets or sets the list of individual client responses.
    /// </summary>
    public List<ClientResponse> Responses { get; set; } = new();

    /// <summary>
    /// Gets the count of successful responses.
    /// </summary>
    public int SuccessCount => Responses.Count(r => r.Success);

    /// <summary>
    /// Gets the count of failed responses (errors or timeouts).
    /// </summary>
    public int FailureCount => Responses.Count(r => !r.Success);

    /// <summary>
    /// Gets whether all responses were successful.
    /// </summary>
    public bool AllSuccessful => Responses.All(r => r.Success);

    /// <summary>
    /// Gets whether any response was successful.
    /// </summary>
    public bool AnySuccessful => Responses.Any(r => r.Success);
}

/// <summary>
/// Represents a response from a single SignalR client.
/// </summary>
public class ClientResponse
{
    /// <summary>
    /// Gets or sets the connection ID that sent the response.
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID associated with the connection (if available).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the actual SignalR response.
    /// </summary>
    public SignalRResponse? Response { get; set; }

    /// <summary>
    /// Gets or sets whether the response was received successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the response failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was received.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
