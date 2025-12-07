using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nesco.SignalRUserManagement.Client.Services;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Options;

namespace Nesco.SignalRUserManagement.Client.Handlers;

/// <summary>
/// Extension methods for registering automatic SignalR handler discovery.
/// </summary>
public static class SignalRHandlerExtensions
{
    /// <summary>
    /// Adds SignalR User Management client with automatic handler discovery.
    /// Scans the entry assembly by default for classes implementing ISignalRHandler.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureClient">Optional: configure client options</param>
    /// <example>
    /// <code>
    /// // Simplest usage - scans entry assembly automatically
    /// builder.Services.AddSignalRUserManagementClientWithHandlers();
    ///
    /// // With client options
    /// builder.Services.AddSignalRUserManagementClientWithHandlers(
    ///     client => client.MaxDirectDataSizeBytes = 64 * 1024
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRUserManagementClientWithHandlers(
        this IServiceCollection services,
        Action<SignalRUserManagementClientOptions>? configureClient = null)
    {
        return services.AddSignalRUserManagementClientWithHandlers(
            assembly: null,
            configureClient: configureClient);
    }

    /// <summary>
    /// Adds SignalR User Management client with automatic handler discovery.
    /// Scans the specified assembly for classes implementing ISignalRHandler.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan for handlers</param>
    /// <param name="configureClient">Optional: configure client options</param>
    /// <example>
    /// <code>
    /// // Scan a specific assembly
    /// builder.Services.AddSignalRUserManagementClientWithHandlers(typeof(Program).Assembly);
    ///
    /// // With client options
    /// builder.Services.AddSignalRUserManagementClientWithHandlers(
    ///     typeof(Program).Assembly,
    ///     client => client.MaxDirectDataSizeBytes = 64 * 1024
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRUserManagementClientWithHandlers(
        this IServiceCollection services,
        Assembly? assembly,
        Action<SignalRUserManagementClientOptions>? configureClient = null)
    {
        var handlerOptions = new SignalRHandlerOptions();
        if (assembly != null)
        {
            handlerOptions.AssembliesToScan.Add(assembly);
        }

        return services.AddSignalRUserManagementClientWithHandlersCore(handlerOptions, configureClient);
    }

    /// <summary>
    /// Adds SignalR User Management client with automatic handler discovery.
    /// Provides full control over handler discovery options.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureHandlers">Configure handler discovery options</param>
    /// <param name="configureClient">Optional: configure client options</param>
    /// <example>
    /// <code>
    /// // Full control over handler options
    /// builder.Services.AddSignalRUserManagementClientWithHandlers(
    ///     handlers =>
    ///     {
    ///         handlers.AssembliesToScan.Add(typeof(Program).Assembly);
    ///         handlers.HandlerLifetime = ServiceLifetime.Transient;
    ///     },
    ///     client => client.MaxDirectDataSizeBytes = 64 * 1024
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRUserManagementClientWithHandlers(
        this IServiceCollection services,
        Action<SignalRHandlerOptions> configureHandlers,
        Action<SignalRUserManagementClientOptions>? configureClient = null)
    {
        var handlerOptions = new SignalRHandlerOptions();
        configureHandlers.Invoke(handlerOptions);

        return services.AddSignalRUserManagementClientWithHandlersCore(handlerOptions, configureClient);
    }

    private static IServiceCollection AddSignalRUserManagementClientWithHandlersCore(
        this IServiceCollection services,
        SignalRHandlerOptions handlerOptions,
        Action<SignalRUserManagementClientOptions>? configureClient = null)
    {
        var clientOptions = new SignalRUserManagementClientOptions();
        configureClient?.Invoke(clientOptions);

        // Configure UserManagementOptions
        services.Configure<UserManagementOptions>(opt =>
        {
            if (!string.IsNullOrEmpty(clientOptions.HubUrl))
                opt.HubUrl = clientOptions.HubUrl;
            opt.ReconnectDelaysSeconds = clientOptions.ReconnectDelaysSeconds;
        });

        // Configure CommunicatorOptions
        services.Configure<CommunicatorOptions>(opt =>
        {
            opt.MaxDirectDataSizeBytes = clientOptions.MaxDirectDataSizeBytes;
            opt.TempFolder = clientOptions.TempFolder;
            opt.FileUploadRoute = clientOptions.FileUploadRoute;
        });

        // Register the connection client
        services.AddSingleton<UserConnectionClient>();

        // Discover and register handler types
        var handlerTypes = DiscoverHandlerTypes(handlerOptions);

        foreach (var handlerType in handlerTypes)
        {
            // Register handler type with the configured lifetime
            var msLifetime = handlerOptions.HandlerLifetime switch
            {
                ServiceLifetime.Singleton => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton,
                ServiceLifetime.Scoped => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped,
                ServiceLifetime.Transient => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient,
                _ => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped
            };
            services.TryAdd(new ServiceDescriptor(handlerType, handlerType, msLifetime));
        }

        // Register the ReflectionMethodExecutor
        services.AddSingleton<ReflectionMethodExecutor>(sp =>
        {
            var executor = ActivatorUtilities.CreateInstance<ReflectionMethodExecutor>(sp);

            // Register discovered handler types
            foreach (var handlerType in handlerTypes)
            {
                executor.RegisterHandlerType(handlerType);
            }

            return executor;
        });

        // Register as IMethodExecutor
        services.AddSingleton<IMethodExecutor>(sp => sp.GetRequiredService<ReflectionMethodExecutor>());

        // Register file upload service if enabled
        if (clientOptions.EnableFileUpload)
        {
            services.TryAddScoped<IFileUploadService, DefaultFileUploadService>();
        }

        return services;
    }

    /// <summary>
    /// Adds a specific handler type to the SignalR handler system.
    /// Call this after AddSignalRUserManagementClientWithHandlers for additional handler registration.
    /// </summary>
    /// <typeparam name="THandler">The handler type to register</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="lifetime">Service lifetime for the handler</param>
    public static IServiceCollection AddSignalRHandler<THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class
    {
        var handlerType = typeof(THandler);

        // Register the handler type
        var msLifetime = lifetime switch
        {
            ServiceLifetime.Singleton => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton,
            ServiceLifetime.Scoped => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped,
            ServiceLifetime.Transient => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient,
            _ => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped
        };
        services.TryAdd(new ServiceDescriptor(handlerType, handlerType, msLifetime));

        // Update the ReflectionMethodExecutor to include this handler
        // This requires a custom factory that will be called when the executor is resolved
        services.AddSingleton<IHandlerTypeProvider>(new HandlerTypeProvider(handlerType));

        return services;
    }

    private static List<Type> DiscoverHandlerTypes(SignalRHandlerOptions options)
    {
        var handlerTypes = new List<Type>();

        // Add explicitly specified handler types
        handlerTypes.AddRange(options.HandlerTypes);

        // Scan assemblies for handler types
        var assembliesToScan = options.AssembliesToScan.Count > 0
            ? options.AssembliesToScan
            : new List<Assembly> { Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() };

        foreach (var assembly in assembliesToScan)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(IsSignalRHandler)
                    .Where(t => !handlerTypes.Contains(t));

                handlerTypes.AddRange(types);
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Handle assemblies that can't be fully loaded
                var loadedTypes = ex.Types
                    .Where(t => t != null)
                    .Cast<Type>()
                    .Where(IsSignalRHandler)
                    .Where(t => !handlerTypes.Contains(t));

                handlerTypes.AddRange(loadedTypes);
            }
        }

        return handlerTypes.Distinct().ToList();
    }

    private static bool IsSignalRHandler(Type type)
    {
        if (type.IsAbstract || type.IsInterface || !type.IsClass)
            return false;

        // Check for ISignalRHandler interface
        if (typeof(ISignalRHandler).IsAssignableFrom(type))
            return true;

        // Check for [SignalRHandler] attribute
        if (type.GetCustomAttribute<SignalRHandlerAttribute>() != null)
            return true;

        return false;
    }
}

/// <summary>
/// Internal interface for providing additional handler types.
/// </summary>
internal interface IHandlerTypeProvider
{
    Type HandlerType { get; }
}

internal class HandlerTypeProvider : IHandlerTypeProvider
{
    public Type HandlerType { get; }

    public HandlerTypeProvider(Type handlerType)
    {
        HandlerType = handlerType;
    }
}
