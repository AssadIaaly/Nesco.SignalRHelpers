using Microsoft.Extensions.DependencyInjection;
using Nesco.SignalRUserManagement.Server.Groups.Interfaces;
using Nesco.SignalRUserManagement.Server.Groups.Services;

namespace Nesco.SignalRUserManagement.Server.Groups.Extensions;

/// <summary>
/// Extension methods for configuring SignalR group management services.
/// </summary>
public static class GroupManagementExtensions
{
    /// <summary>
    /// Adds SignalR group management services to the service collection.
    /// This enables group tracking and management features in UserManagementHubWithGroups.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// builder.Services.AddSignalRGroupManagement();
    ///
    /// // Then use UserManagementHubWithGroups instead of UserManagementHub
    /// app.MapHub&lt;UserManagementHubWithGroups&gt;("/hubs/usermanagement");
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRGroupManagement(this IServiceCollection services)
    {
        services.AddSingleton<IGroupManager, InMemoryGroupManager>();
        return services;
    }

    /// <summary>
    /// Adds SignalR group management services with a custom IGroupManager implementation.
    /// Use this if you want to provide your own group tracking logic (e.g., database-backed).
    /// </summary>
    /// <typeparam name="TGroupManager">The type implementing IGroupManager</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// builder.Services.AddSignalRGroupManagement&lt;MyCustomGroupManager&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRGroupManagement<TGroupManager>(
        this IServiceCollection services)
        where TGroupManager : class, IGroupManager
    {
        services.AddSingleton<IGroupManager, TGroupManager>();
        return services;
    }
}
