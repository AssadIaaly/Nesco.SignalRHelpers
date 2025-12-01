using Nesco.SignalRUserManagement.Core.Models;

namespace Nesco.SignalRUserManagement.Core.Interfaces;

/// <summary>
/// Service for invoking methods on connected SignalR clients and receiving responses.
/// </summary>
public interface IUserCommunicatorService
{
    /// <summary>
    /// Invokes a method on all connected clients and waits for the first response.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The response from the client.</returns>
    Task<SignalRResponse> InvokeMethodAsync(string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on all connected clients, waits for response, and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The deserialized response, or null if the response was null or an error occurred.</returns>
    Task<T?> InvokeMethodAsync<T>(string methodName, object? parameter) where T : class;

    /// <summary>
    /// Invokes a method on a specific connection and waits for response.
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID to target.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The response from the client.</returns>
    Task<SignalRResponse> InvokeMethodOnConnectionAsync(string connectionId, string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on a specific connection, waits for response, and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="connectionId">The SignalR connection ID to target.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The deserialized response, or null if the response was null or an error occurred.</returns>
    Task<T?> InvokeMethodOnConnectionAsync<T>(string connectionId, string methodName, object? parameter) where T : class;

    /// <summary>
    /// Invokes a method on all connections of a specific user and waits for the first response.
    /// </summary>
    /// <param name="userId">The user ID to target (all their connections).</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The response from the client.</returns>
    Task<SignalRResponse> InvokeMethodOnUserAsync(string userId, string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on all connections of a specific user, waits for response, and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="userId">The user ID to target (all their connections).</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The deserialized response, or null if the response was null or an error occurred.</returns>
    Task<T?> InvokeMethodOnUserAsync<T>(string userId, string methodName, object? parameter) where T : class;

    /// <summary>
    /// Invokes a method on all connections of multiple users and waits for the first response.
    /// </summary>
    /// <param name="userIds">The user IDs to target (all their connections).</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The response from the client.</returns>
    Task<SignalRResponse> InvokeMethodOnUsersAsync(IEnumerable<string> userIds, string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on all connections of multiple users, waits for response, and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="userIds">The user IDs to target (all their connections).</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The deserialized response, or null if the response was null or an error occurred.</returns>
    Task<T?> InvokeMethodOnUsersAsync<T>(IEnumerable<string> userIds, string methodName, object? parameter) where T : class;

    /// <summary>
    /// Invokes a method on a list of specific connections and waits for the first response.
    /// </summary>
    /// <param name="connectionIds">The SignalR connection IDs to target.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The response from the client.</returns>
    Task<SignalRResponse> InvokeMethodOnConnectionsAsync(IEnumerable<string> connectionIds, string methodName, object? parameter);

    /// <summary>
    /// Invokes a method on a list of specific connections, waits for response, and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="connectionIds">The SignalR connection IDs to target.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <returns>The deserialized response, or null if the response was null or an error occurred.</returns>
    Task<T?> InvokeMethodOnConnectionsAsync<T>(IEnumerable<string> connectionIds, string methodName, object? parameter) where T : class;

    // ===== Streaming Multi-Response Methods =====

    /// <summary>
    /// Invokes a method on all specified connections and streams responses as they arrive.
    /// </summary>
    /// <param name="connectionIds">The list of connection IDs to target.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameter">The parameter to pass to the method.</param>
    /// <param name="cancellationToken">Cancellation token to stop receiving responses.</param>
    /// <returns>An async enumerable that yields responses as they arrive from each client.</returns>
    IAsyncEnumerable<ClientResponse> InvokeMethodStreamingAsync(
        IEnumerable<string> connectionIds,
        string methodName,
        object? parameter,
        CancellationToken cancellationToken = default);
}
