using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nesco.SignalRUserManagement.Client.Authorization.Services;

namespace Nesco.SignalRUserManagement.Client.Authorization.Extensions;

/// <summary>
/// Extension methods for configuring SignalR Client Authorization services
/// </summary>
public static class ClientAuthExtensions
{
    /// <summary>
    /// Adds SignalR Client Authorization services for Blazor WebAssembly.
    /// Uses <see cref="LocalStorageAuthTokenStorage"/> as the default storage mechanism.
    /// Includes AuthService, AuthenticationStateProvider, and AuthorizationCore.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignalRClientAuth(this IServiceCollection services)
    {
        // Register the default localStorage-based token storage for Blazor WASM
        services.TryAddScoped<IAuthTokenStorage, LocalStorageAuthTokenStorage>();

        return services.AddSignalRClientAuthCore();
    }

    /// <summary>
    /// Adds SignalR Client Authorization services with a custom token storage implementation.
    /// Use this overload for MAUI apps or other platforms that need different storage mechanisms.
    /// </summary>
    /// <typeparam name="TStorage">The custom storage implementation type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// For MAUI with Preferences:
    /// <code>
    /// services.AddSignalRClientAuth&lt;PreferencesAuthTokenStorage&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRClientAuth<TStorage>(this IServiceCollection services)
        where TStorage : class, IAuthTokenStorage
    {
        // Register the custom token storage implementation
        services.TryAddScoped<IAuthTokenStorage, TStorage>();

        return services.AddSignalRClientAuthCore();
    }

    /// <summary>
    /// Core registration of auth services (shared by all overloads)
    /// </summary>
    private static IServiceCollection AddSignalRClientAuthCore(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<AuthenticationStateProvider, SignalRAuthStateProvider>();
        services.AddAuthorizationCore();
        return services;
    }
}
