using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Nesco.SignalRUserManagement.Client.Authorization.Services;

namespace Nesco.SignalRUserManagement.Client.Authorization.Extensions;

/// <summary>
/// Extension methods for configuring SignalR Client Authorization services
/// </summary>
public static class ClientAuthExtensions
{
    /// <summary>
    /// Adds SignalR Client Authorization services for Blazor WebAssembly.
    /// Includes AuthService, AuthenticationStateProvider, and AuthorizationCore.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignalRClientAuth(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<AuthenticationStateProvider, SignalRAuthStateProvider>();
        services.AddAuthorizationCore();
        return services;
    }
}
