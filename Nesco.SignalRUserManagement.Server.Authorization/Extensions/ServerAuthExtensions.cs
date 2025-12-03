using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Nesco.SignalRUserManagement.Server.Authorization.Providers;

namespace Nesco.SignalRUserManagement.Server.Authorization.Extensions;

/// <summary>
/// Extension methods for configuring SignalR JWT Authorization services
/// </summary>
public static class ServerAuthExtensions
{
    /// <summary>
    /// Adds the SignalR UserIdProvider that extracts user ID from JWT claims.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignalRUserIdProvider(this IServiceCollection services)
    {
        services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
        return services;
    }
}
