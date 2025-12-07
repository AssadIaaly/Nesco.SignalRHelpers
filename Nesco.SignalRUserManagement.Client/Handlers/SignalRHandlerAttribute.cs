namespace Nesco.SignalRUserManagement.Client.Handlers;

/// <summary>
/// Marks a class as a SignalR handler for automatic discovery.
/// This is an alternative to implementing ISignalRHandler.
/// </summary>
/// <example>
/// <code>
/// [SignalRHandler]
/// public class NotificationHandler
/// {
///     public Task&lt;object?&gt; ShowAlert(AlertDto alert)
///     {
///         // Handle the alert
///         return Task.FromResult&lt;object?&gt;(new { shown = true });
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SignalRHandlerAttribute : Attribute
{
}

/// <summary>
/// Specifies a custom method name for a SignalR handler method.
/// Use this when the SignalR method name differs from the C# method name.
/// </summary>
/// <example>
/// <code>
/// public class MyHandler : ISignalRHandler
/// {
///     [SignalRMethod("ServerMessage")]
///     public Task&lt;object?&gt; HandleServerMessage(MessageDto message)
///     {
///         // This handles "ServerMessage" from the server
///         return Task.FromResult&lt;object?&gt;(new { received = true });
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SignalRMethodAttribute : Attribute
{
    /// <summary>
    /// The SignalR method name this handler responds to.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Creates a new SignalRMethodAttribute with the specified method name.
    /// </summary>
    /// <param name="methodName">The SignalR method name to handle.</param>
    public SignalRMethodAttribute(string methodName)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }
}

/// <summary>
/// Excludes a method from being registered as a SignalR handler.
/// Use this on public methods that should not handle SignalR messages.
/// </summary>
/// <example>
/// <code>
/// public class MyHandler : ISignalRHandler
/// {
///     public Task&lt;object?&gt; HandleMessage(MessageDto message)
///     {
///         return ProcessMessage(message);
///     }
///
///     [SignalRIgnore]
///     public Task&lt;object?&gt; ProcessMessage(MessageDto message)
///     {
///         // This is a helper method, not a handler
///         return Task.FromResult&lt;object?&gt;(new { processed = true });
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SignalRIgnoreAttribute : Attribute
{
}
