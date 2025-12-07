namespace Nesco.SignalRUserManagement.Client.Handlers;

/// <summary>
/// Marker interface for SignalR handler classes.
/// Classes implementing this interface will be auto-discovered and their public methods
/// will be registered as SignalR method handlers.
///
/// Handler methods should:
/// - Be public instance methods
/// - Have a name matching the SignalR method name to handle
/// - Accept a single parameter that will be deserialized from the SignalR message
/// - Return Task&lt;object?&gt; or object? (sync methods are wrapped automatically)
/// </summary>
/// <example>
/// <code>
/// public class ChatHandler : ISignalRHandler
/// {
///     public Task&lt;object?&gt; ReceiveMessage(MessageDto message)
///     {
///         // Handle the message
///         return Task.FromResult&lt;object?&gt;(new { received = true });
///     }
///
///     public object? Ping(PingRequest request)
///     {
///         return new { pong = true, timestamp = DateTime.UtcNow };
///     }
/// }
/// </code>
/// </example>
public interface ISignalRHandler
{
}
