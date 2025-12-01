namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Interface for resolving user names from user IDs.
/// Implement this interface to display user names in the dashboard.
/// </summary>
public interface IUserNameResolver
{
    /// <summary>
    /// Resolves user names for a collection of user IDs
    /// </summary>
    /// <param name="userIds">The user IDs to resolve</param>
    /// <returns>Dictionary mapping user IDs to user names</returns>
    Task<Dictionary<string, string>> ResolveUserNamesAsync(IEnumerable<string> userIds);
}
