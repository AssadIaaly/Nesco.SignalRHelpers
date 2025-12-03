namespace Nesco.SignalRUserManagement.Client.Authorization.Services;

/// <summary>
/// Interface for persisting authentication tokens and user data.
/// Implement this interface to provide custom storage mechanisms (e.g., localStorage, SecureStorage, Preferences).
/// </summary>
public interface IAuthTokenStorage
{
    /// <summary>
    /// Retrieves the stored authentication token.
    /// </summary>
    /// <returns>The stored token, or null if not found.</returns>
    Task<string?> GetTokenAsync();

    /// <summary>
    /// Retrieves the stored user ID.
    /// </summary>
    /// <returns>The stored user ID, or null if not found.</returns>
    Task<string?> GetUserIdAsync();

    /// <summary>
    /// Retrieves the stored user email.
    /// </summary>
    /// <returns>The stored email, or null if not found.</returns>
    Task<string?> GetEmailAsync();

    /// <summary>
    /// Stores the authentication data.
    /// </summary>
    /// <param name="token">The authentication token.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="email">The user email.</param>
    Task SaveAsync(string? token, string userId, string email);

    /// <summary>
    /// Clears all stored authentication data.
    /// </summary>
    Task ClearAsync();
}
