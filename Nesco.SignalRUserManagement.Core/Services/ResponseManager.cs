using System.Collections.Concurrent;
using System.Threading.Channels;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Models;

namespace Nesco.SignalRUserManagement.Core.Services;

/// <summary>
/// Thread-safe manager for pending SignalR requests.
/// </summary>
public class ResponseManager : IResponseManager
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<SignalRResponse>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, StreamingRequestState> _streamingRequests = new();

    /// <inheritdoc/>
    public void RegisterRequest(string requestId, TaskCompletionSource<SignalRResponse> tcs)
    {
        if (!_pendingRequests.TryAdd(requestId, tcs))
        {
            throw new InvalidOperationException($"Request {requestId} is already registered");
        }
    }

    /// <inheritdoc/>
    public ChannelReader<ClientResponse> RegisterStreamingRequest(string requestId, int expectedCount)
    {
        // Use unbounded channel so writes never block
        var channel = Channel.CreateUnbounded<ClientResponse>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var state = new StreamingRequestState
        {
            ExpectedCount = expectedCount,
            Channel = channel,
            ReceivedCount = 0
        };

        if (!_streamingRequests.TryAdd(requestId, state))
        {
            throw new InvalidOperationException($"Streaming request {requestId} is already registered");
        }

        return channel.Reader;
    }

    /// <inheritdoc/>
    public bool CompleteRequest(string requestId, SignalRResponse response)
    {
        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(response);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool AddStreamingResponse(string requestId, string connectionId, SignalRResponse response)
    {
        if (!_streamingRequests.TryGetValue(requestId, out var state))
        {
            return false;
        }

        var clientResponse = new ClientResponse
        {
            ConnectionId = connectionId,
            Response = response,
            Success = response.ResponseType != SignalRResponseType.Error,
            ErrorMessage = response.ErrorMessage,
            ReceivedAt = DateTime.UtcNow
        };

        // Write to channel - this makes it immediately available to consumers
        state.Channel.Writer.TryWrite(clientResponse);

        // Track received count and auto-complete if all received
        var newCount = Interlocked.Increment(ref state.ReceivedCount);
        if (newCount >= state.ExpectedCount)
        {
            CompleteStreamingRequest(requestId);
        }

        return true;
    }

    /// <inheritdoc/>
    public void CompleteStreamingRequest(string requestId)
    {
        if (_streamingRequests.TryRemove(requestId, out var state))
        {
            // Complete the channel - signals to readers that no more items will be written
            state.Channel.Writer.TryComplete();
        }
    }

    /// <inheritdoc/>
    public void RemoveRequest(string requestId)
    {
        _pendingRequests.TryRemove(requestId, out _);

        if (_streamingRequests.TryRemove(requestId, out var state))
        {
            state.Channel.Writer.TryComplete();
        }
    }

    private class StreamingRequestState
    {
        public int ExpectedCount { get; set; }
        public Channel<ClientResponse> Channel { get; set; } = null!;
        public int ReceivedCount;
    }
}
