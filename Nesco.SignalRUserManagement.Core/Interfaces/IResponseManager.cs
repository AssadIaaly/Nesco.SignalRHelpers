using System.Threading.Channels;
using Nesco.SignalRUserManagement.Core.Models;

namespace Nesco.SignalRUserManagement.Core.Interfaces;

/// <summary>
/// Manages pending requests and their completion for method invocation responses.
/// </summary>
public interface IResponseManager
{
    /// <summary>
    /// Registers a new pending request expecting a single response.
    /// </summary>
    /// <param name="requestId">Unique identifier for the request.</param>
    /// <param name="tcs">TaskCompletionSource to complete when response arrives.</param>
    void RegisterRequest(string requestId, TaskCompletionSource<SignalRResponse> tcs);

    /// <summary>
    /// Registers a streaming multi-response request that yields results as they arrive.
    /// </summary>
    /// <param name="requestId">Unique identifier for the request.</param>
    /// <param name="expectedCount">The expected number of responses.</param>
    /// <returns>A channel reader that yields ClientResponse objects as they arrive.</returns>
    ChannelReader<ClientResponse> RegisterStreamingRequest(string requestId, int expectedCount);

    /// <summary>
    /// Completes a pending single-response request with the given response.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="response">The response from the client.</param>
    /// <returns>True if the request was found and completed, false otherwise.</returns>
    bool CompleteRequest(string requestId, SignalRResponse response);

    /// <summary>
    /// Adds a response to a streaming multi-response request.
    /// The response will be immediately available to consumers reading from the channel.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="connectionId">The connection ID that sent the response.</param>
    /// <param name="response">The response from the client.</param>
    /// <returns>True if the response was added, false if request not found.</returns>
    bool AddStreamingResponse(string requestId, string connectionId, SignalRResponse response);

    /// <summary>
    /// Marks a streaming request as complete (no more responses expected).
    /// This closes the channel so consumers know to stop reading.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    void CompleteStreamingRequest(string requestId);

    /// <summary>
    /// Removes a pending request without completing it.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    void RemoveRequest(string requestId);
}
