using System.Reflection;

namespace Nesco.SignalRUserManagement.Client.Handlers;

/// <summary>
/// Configuration options for automatic SignalR handler discovery.
/// </summary>
public class SignalRHandlerOptions
{
    /// <summary>
    /// Assemblies to scan for handler classes.
    /// If empty, the calling assembly will be scanned.
    /// </summary>
    public List<Assembly> AssembliesToScan { get; } = new();

    /// <summary>
    /// Specific handler types to register.
    /// Use this for explicit registration instead of assembly scanning.
    /// </summary>
    public List<Type> HandlerTypes { get; } = new();

    /// <summary>
    /// Whether to throw an exception when no handler is found for a method.
    /// Default is true. If false, an error response will be returned instead.
    /// </summary>
    public bool ThrowOnMissingHandler { get; set; } = true;

    /// <summary>
    /// Whether to use case-insensitive method name matching.
    /// Default is true.
    /// </summary>
    public bool CaseInsensitiveMethodNames { get; set; } = true;

    /// <summary>
    /// ServiceLifetime for handler registration.
    /// Default is Scoped to allow per-request dependencies.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Scoped;
}

/// <summary>
/// Represents the service lifetime for handler registration.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// A new instance is created each time.
    /// </summary>
    Transient,

    /// <summary>
    /// A new instance is created per scope (e.g., per request).
    /// This is the default and recommended option.
    /// </summary>
    Scoped,

    /// <summary>
    /// A single instance is shared for the lifetime of the application.
    /// </summary>
    Singleton
}
