using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nesco.SignalRUserManagement.Client.Services;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Options;

namespace Nesco.SignalRUserManagement.Client.Extensions;

/// <summary>
/// Extension methods for configuring SignalR User Management on the client
/// </summary>
public static class UserManagementClientExtensions
{
    /// <summary>
    /// Adds SignalR User Management client services with method invocation support.
    /// This is the recommended way to configure the client.
    /// </summary>
    /// <typeparam name="TMethodExecutor">The type implementing IMethodExecutor that handles method invocations from the server.</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration</param>
    /// <example>
    /// <code>
    /// // In Program.cs - Single call to configure everything
    /// builder.Services.AddSignalRUserManagementClient&lt;MyMethodExecutor&gt;(options =>
    /// {
    ///     options.HubUrl = "https://myserver.com/hubs/usermanagement";
    ///     options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
    ///     options.EnableFileUpload = true; // Enable file upload for large responses
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRUserManagementClient<TMethodExecutor>(
        this IServiceCollection services,
        Action<SignalRUserManagementClientOptions>? configure = null)
        where TMethodExecutor : class, IMethodExecutor
    {
        var options = new SignalRUserManagementClientOptions();
        configure?.Invoke(options);

        // Configure UserManagementOptions (for backward compatibility with UserConnectionClient)
        services.Configure<UserManagementOptions>(opt =>
        {
            if (!string.IsNullOrEmpty(options.HubUrl))
                opt.HubUrl = options.HubUrl;
            opt.ReconnectDelaysSeconds = options.ReconnectDelaysSeconds;
        });

        // Configure CommunicatorOptions
        services.Configure<CommunicatorOptions>(opt =>
        {
            opt.MaxDirectDataSizeBytes = options.MaxDirectDataSizeBytes;
            opt.TempFolder = options.TempFolder;
            opt.FileUploadRoute = options.FileUploadRoute;
        });

        // Register the connection client
        services.AddSingleton<UserConnectionClient>();

        // Register the method executor
        services.AddSingleton<IMethodExecutor, TMethodExecutor>();

        // Register file upload service if enabled (requires HttpClient to be registered)
        // Using Scoped lifetime to match HttpClient's typical registration in Blazor WASM
        if (options.EnableFileUpload)
        {
            services.TryAddScoped<IFileUploadService, DefaultFileUploadService>();
        }

        return services;
    }

    /// <summary>
    /// Adds SignalR User Management client services without method invocation support.
    /// Use this when you only need connection management without server-initiated method calls.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration</param>
    public static IServiceCollection AddSignalRUserManagementClient(
        this IServiceCollection services,
        Action<SignalRUserManagementClientOptions>? configure = null)
    {
        var options = new SignalRUserManagementClientOptions();
        configure?.Invoke(options);

        services.Configure<UserManagementOptions>(opt =>
        {
            if (!string.IsNullOrEmpty(options.HubUrl))
                opt.HubUrl = options.HubUrl;
            opt.ReconnectDelaysSeconds = options.ReconnectDelaysSeconds;
        });

        services.Configure<CommunicatorOptions>(opt =>
        {
            opt.MaxDirectDataSizeBytes = options.MaxDirectDataSizeBytes;
            opt.TempFolder = options.TempFolder;
            opt.FileUploadRoute = options.FileUploadRoute;
        });

        services.AddSingleton<UserConnectionClient>();

        // Register file upload service if enabled (requires HttpClient to be registered)
        // Using Scoped lifetime to match HttpClient's typical registration in Blazor WASM
        if (options.EnableFileUpload)
        {
            services.TryAddScoped<IFileUploadService, DefaultFileUploadService>();
        }

        return services;
    }

    #region Legacy Methods (for backward compatibility)

    /// <summary>
    /// Adds SignalR User Management client services.
    /// Consider using AddSignalRUserManagementClient instead for simpler configuration.
    /// </summary>
    [Obsolete("Use AddSignalRUserManagementClient<TMethodExecutor>() instead for simpler configuration.")]
    public static IServiceCollection AddUserManagementClient(
        this IServiceCollection services,
        string? hubUrl = null,
        Action<UserManagementOptions>? configure = null)
    {
        services.Configure<UserManagementOptions>(options =>
        {
            if (!string.IsNullOrEmpty(hubUrl))
                options.HubUrl = hubUrl;
            configure?.Invoke(options);
        });

        services.AddSingleton<UserConnectionClient>();

        return services;
    }

    /// <summary>
    /// Adds method invocation (communicator) support to the User Management client.
    /// Consider using AddSignalRUserManagementClient with a method executor type instead.
    /// </summary>
    [Obsolete("Use AddSignalRUserManagementClient<TMethodExecutor>() instead.")]
    public static IServiceCollection AddUserManagementClientCommunicator<TMethodExecutor>(
        this IServiceCollection services,
        Action<CommunicatorOptions>? configure = null)
        where TMethodExecutor : class, IMethodExecutor
    {
        var options = new CommunicatorOptions();
        configure?.Invoke(options);

        services.Configure<CommunicatorOptions>(opt =>
        {
            opt.MaxConcurrentRequests = options.MaxConcurrentRequests;
            opt.RequestTimeoutSeconds = options.RequestTimeoutSeconds;
            opt.SemaphoreTimeoutSeconds = options.SemaphoreTimeoutSeconds;
            opt.MaxDirectDataSizeBytes = options.MaxDirectDataSizeBytes;
            opt.AutoDeleteTempFiles = options.AutoDeleteTempFiles;
        });

        services.AddSingleton<IMethodExecutor, TMethodExecutor>();

        return services;
    }

    #endregion
}
