using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Options;
using Nesco.SignalRUserManagement.Core.Services;
using Nesco.SignalRUserManagement.Server.Hubs;
using Nesco.SignalRUserManagement.Server.Services;

namespace Nesco.SignalRUserManagement.Server.Extensions;

/// <summary>
/// Extension methods for configuring SignalR User Management on the server.
/// No database required - uses in-memory connection tracking.
/// </summary>
public static class UserManagementServerExtensions
{
    /// <summary>
    /// Adds all SignalR User Management services with a single call.
    /// Uses in-memory connection tracking - no database required.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration</param>
    /// <example>
    /// <code>
    /// // In Program.cs - Add all services with one call
    /// builder.Services.AddSignalRUserManagement(options =>
    /// {
    ///     options.HubPath = "/hubs/usermanagement";
    ///     options.EnableCommunicator = true;
    ///     options.EnableDashboard = true;
    ///     options.RequestTimeoutSeconds = 30;
    /// });
    ///
    /// // Map the hub with one call
    /// app.MapSignalRUserManagement();
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRUserManagement(
        this IServiceCollection services,
        Action<SignalRUserManagementOptions>? configure = null)
    {
        var options = new SignalRUserManagementOptions();
        configure?.Invoke(options);

        // Store options for later use
        services.Configure<SignalRUserManagementOptions>(opt =>
        {
            opt.HubPath = options.HubPath;
            opt.KeepAliveIntervalSeconds = options.KeepAliveIntervalSeconds;
            opt.ClientTimeoutSeconds = options.ClientTimeoutSeconds;
            opt.ReconnectDelaysSeconds = options.ReconnectDelaysSeconds;
            opt.EnableCommunicator = options.EnableCommunicator;
            opt.EnableDashboard = options.EnableDashboard;
            opt.MaxConcurrentRequests = options.MaxConcurrentRequests;
            opt.RequestTimeoutSeconds = options.RequestTimeoutSeconds;
            opt.SemaphoreTimeoutSeconds = options.SemaphoreTimeoutSeconds;
            opt.MaxDirectDataSizeBytes = options.MaxDirectDataSizeBytes;
            opt.AutoDeleteTempFiles = options.AutoDeleteTempFiles;
        });

        // Configure SignalR
        services.AddSignalR(signalr =>
        {
            signalr.KeepAliveInterval = TimeSpan.FromSeconds(options.KeepAliveIntervalSeconds);
            signalr.ClientTimeoutInterval = TimeSpan.FromSeconds(options.ClientTimeoutSeconds);
        });

        // Register in-memory connection tracker (singleton)
        services.TryAddSingleton<InMemoryConnectionTracker>();

        // Register core user connection service
        services.AddScoped<IUserConnectionService, UserConnectionService<UserManagementHub>>();

        // Add communicator if enabled
        if (options.EnableCommunicator)
        {
            services.Configure<CommunicatorOptions>(opt =>
            {
                opt.MaxConcurrentRequests = options.MaxConcurrentRequests;
                opt.RequestTimeoutSeconds = options.RequestTimeoutSeconds;
                opt.SemaphoreTimeoutSeconds = options.SemaphoreTimeoutSeconds;
                opt.MaxDirectDataSizeBytes = options.MaxDirectDataSizeBytes;
                opt.AutoDeleteTempFiles = options.AutoDeleteTempFiles;
                opt.TempFolder = options.TempFolder;
                opt.MaxFileSize = options.MaxFileSize;
                opt.FileUploadRoute = options.FileUploadRoute;
            });

            services.TryAddSingleton<IResponseManager, ResponseManager>();

            // Register file reader service for handling FilePath responses
            services.TryAddSingleton<IFileReaderService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultFileReaderService>>();
                var env = sp.GetService<IWebHostEnvironment>();
                return new DefaultFileReaderService(logger, env?.WebRootPath);
            });

            services.AddScoped<IUserCommunicatorService, UserCommunicatorService<UserManagementHub>>();
        }

        // Add unified management service (dashboard support)
        if (options.EnableDashboard)
        {
            if (!options.EnableCommunicator)
            {
                services.TryAddSingleton<IFileReaderService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DefaultFileReaderService>>();
                    var env = sp.GetService<IWebHostEnvironment>();
                    return new DefaultFileReaderService(logger, env?.WebRootPath);
                });
            }

            services.AddScoped<ISignalRUserManagementService, SignalRUserManagementService>();
        }

        return services;
    }

    /// <summary>
    /// Adds all SignalR User Management services with a custom hub type.
    /// Use this overload when you have a custom hub that inherits from UserManagementHub.
    /// </summary>
    /// <typeparam name="THub">Custom hub type that inherits from UserManagementHub</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration</param>
    /// <example>
    /// <code>
    /// // Create a custom hub with additional methods
    /// public class AppHub : UserManagementHub
    /// {
    ///     public AppHub(InMemoryConnectionTracker tracker, ILogger&lt;UserManagementHub&gt; logger,
    ///         IResponseManager? responseManager = null) : base(tracker, logger, responseManager) { }
    ///
    ///     public Task&lt;string&gt; GetServerTime() => Task.FromResult(DateTime.UtcNow.ToString("O"));
    /// }
    ///
    /// // In Program.cs - Add all services with custom hub type
    /// builder.Services.AddSignalRUserManagement&lt;AppHub&gt;(options =>
    /// {
    ///     options.EnableCommunicator = true;
    ///     options.EnableDashboard = true;
    /// });
    ///
    /// // Map the custom hub
    /// app.MapSignalRUserManagement&lt;AppHub&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRUserManagement<THub>(
        this IServiceCollection services,
        Action<SignalRUserManagementOptions>? configure = null)
        where THub : UserManagementHub
    {
        var options = new SignalRUserManagementOptions();
        configure?.Invoke(options);

        // Store options for later use
        services.Configure<SignalRUserManagementOptions>(opt =>
        {
            opt.HubPath = options.HubPath;
            opt.KeepAliveIntervalSeconds = options.KeepAliveIntervalSeconds;
            opt.ClientTimeoutSeconds = options.ClientTimeoutSeconds;
            opt.ReconnectDelaysSeconds = options.ReconnectDelaysSeconds;
            opt.EnableCommunicator = options.EnableCommunicator;
            opt.EnableDashboard = options.EnableDashboard;
            opt.MaxConcurrentRequests = options.MaxConcurrentRequests;
            opt.RequestTimeoutSeconds = options.RequestTimeoutSeconds;
            opt.SemaphoreTimeoutSeconds = options.SemaphoreTimeoutSeconds;
            opt.MaxDirectDataSizeBytes = options.MaxDirectDataSizeBytes;
            opt.AutoDeleteTempFiles = options.AutoDeleteTempFiles;
        });

        // Configure SignalR
        services.AddSignalR(signalr =>
        {
            signalr.KeepAliveInterval = TimeSpan.FromSeconds(options.KeepAliveIntervalSeconds);
            signalr.ClientTimeoutInterval = TimeSpan.FromSeconds(options.ClientTimeoutSeconds);
        });

        // Register in-memory connection tracker (singleton)
        services.TryAddSingleton<InMemoryConnectionTracker>();

        // Register core user connection service with the custom hub type
        services.AddScoped<IUserConnectionService, UserConnectionService<THub>>();

        // Add communicator if enabled - uses the custom hub type for correct IHubContext routing
        if (options.EnableCommunicator)
        {
            services.Configure<CommunicatorOptions>(opt =>
            {
                opt.MaxConcurrentRequests = options.MaxConcurrentRequests;
                opt.RequestTimeoutSeconds = options.RequestTimeoutSeconds;
                opt.SemaphoreTimeoutSeconds = options.SemaphoreTimeoutSeconds;
                opt.MaxDirectDataSizeBytes = options.MaxDirectDataSizeBytes;
                opt.AutoDeleteTempFiles = options.AutoDeleteTempFiles;
                opt.TempFolder = options.TempFolder;
                opt.MaxFileSize = options.MaxFileSize;
                opt.FileUploadRoute = options.FileUploadRoute;
            });

            services.TryAddSingleton<IResponseManager, ResponseManager>();

            // Register file reader service for handling FilePath responses
            services.TryAddSingleton<IFileReaderService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultFileReaderService>>();
                var env = sp.GetService<IWebHostEnvironment>();
                return new DefaultFileReaderService(logger, env?.WebRootPath);
            });

            // Register communicator with the custom hub type - this is critical for correct message routing
            services.AddScoped<IUserCommunicatorService, UserCommunicatorService<THub>>();
        }

        // Add unified management service (dashboard support)
        if (options.EnableDashboard)
        {
            if (!options.EnableCommunicator)
            {
                services.TryAddSingleton<IFileReaderService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DefaultFileReaderService>>();
                    var env = sp.GetService<IWebHostEnvironment>();
                    return new DefaultFileReaderService(logger, env?.WebRootPath);
                });
            }

            services.AddScoped<ISignalRUserManagementService, SignalRUserManagementService>();
        }

        return services;
    }

    /// <summary>
    /// Maps the SignalR User Management Hub. Uses the HubPath from options or the specified pattern.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder</param>
    /// <param name="pattern">Optional hub path override. If null, uses value from AddSignalRUserManagement options.</param>
    public static HubEndpointConventionBuilder MapSignalRUserManagement(
        this IEndpointRouteBuilder endpoints,
        string? pattern = null)
    {
        var finalPattern = pattern ?? "/hubs/usermanagement";
        return endpoints.MapHub<UserManagementHub>(finalPattern);
    }

    /// <summary>
    /// Maps a custom hub that inherits from UserManagementHub.
    /// Use this when you need to add custom methods to the hub.
    /// </summary>
    /// <typeparam name="THub">Custom hub type that inherits from UserManagementHub</typeparam>
    /// <param name="endpoints">Endpoint route builder</param>
    /// <param name="pattern">Optional hub path override. Defaults to /hubs/usermanagement.</param>
    /// <example>
    /// <code>
    /// // Create a custom hub with additional methods
    /// public class AppHub : UserManagementHub
    /// {
    ///     public AppHub(InMemoryConnectionTracker tracker, ILogger&lt;UserManagementHub&gt; logger,
    ///         IResponseManager? responseManager = null) : base(tracker, logger, responseManager) { }
    ///
    ///     public Task&lt;string&gt; GetServerTime() => Task.FromResult(DateTime.UtcNow.ToString("O"));
    /// }
    ///
    /// // Map the custom hub
    /// app.MapSignalRUserManagement&lt;AppHub&gt;();
    /// </code>
    /// </example>
    public static HubEndpointConventionBuilder MapSignalRUserManagement<THub>(
        this IEndpointRouteBuilder endpoints,
        string? pattern = null)
        where THub : Hub
    {
        var finalPattern = pattern ?? "/hubs/usermanagement";
        return endpoints.MapHub<THub>(finalPattern);
    }
}
