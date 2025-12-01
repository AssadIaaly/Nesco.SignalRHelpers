using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Options;
using Nesco.SignalRUserManagement.Core.Services;
using Nesco.SignalRUserManagement.Server.Hubs;
using Nesco.SignalRUserManagement.Server.Models;
using Nesco.SignalRUserManagement.Server.Services;

namespace Nesco.SignalRUserManagement.Server.Extensions;

/// <summary>
/// Extension methods for configuring SignalR User Management on the server
/// </summary>
public static class UserManagementServerExtensions
{
    /// <summary>
    /// Adds all SignalR User Management services with a single call.
    /// This is the recommended way to configure the library.
    /// </summary>
    /// <typeparam name="TDbContext">DbContext with UserConnections DbSet that implements IUserConnectionDbContext</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration</param>
    /// <example>
    /// <code>
    /// // In Program.cs - Add all services with one call
    /// builder.Services.AddSignalRUserManagement&lt;ApplicationDbContext&gt;(options =>
    /// {
    ///     options.HubPath = "/hubs/usermanagement";
    ///     options.EnableCommunicator = true;
    ///     options.EnableDashboard = true;
    ///     options.RequestTimeoutSeconds = 30;
    /// });
    ///
    /// // Map the hub with one call
    /// app.MapSignalRUserManagement&lt;ApplicationDbContext&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRUserManagement<TDbContext>(
        this IServiceCollection services,
        Action<SignalRUserManagementOptions>? configure = null)
        where TDbContext : DbContext, IUserConnectionDbContext
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

        // Register core user connection service
        services.AddScoped<IUserConnectionService, UserConnectionService<UserManagementHub<TDbContext>, TDbContext>>();

        // Register IUserConnectionDbContext
        services.TryAddScoped<IUserConnectionDbContext>(sp => sp.GetRequiredService<TDbContext>());

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

            services.AddScoped<IUserCommunicatorService, UserCommunicatorService<UserManagementHub<TDbContext>>>();
        }

        // Add unified management service (dashboard support)
        if (options.EnableDashboard)
        {
            // Note: IFileReaderService is already registered above when EnableCommunicator is true
            // If dashboard is enabled but communicator is not, register file reader service here too
            if (!options.EnableCommunicator)
            {
                services.TryAddSingleton<IFileReaderService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DefaultFileReaderService>>();
                    var env = sp.GetService<IWebHostEnvironment>();
                    return new DefaultFileReaderService(logger, env?.WebRootPath);
                });
            }

            services.AddScoped<ISignalRUserManagementService, SignalRUserManagementService<TDbContext>>();
        }

        return services;
    }

    /// <summary>
    /// Maps the SignalR User Management Hub. Uses the HubPath from options or the specified pattern.
    /// </summary>
    /// <typeparam name="TDbContext">DbContext type</typeparam>
    /// <param name="endpoints">Endpoint route builder</param>
    /// <param name="pattern">Optional hub path override. If null, uses value from AddSignalRUserManagement options.</param>
    public static HubEndpointConventionBuilder MapSignalRUserManagement<TDbContext>(
        this IEndpointRouteBuilder endpoints,
        string? pattern = null)
        where TDbContext : DbContext
    {
        var finalPattern = pattern ?? "/hubs/usermanagement";
        return endpoints.MapHub<UserManagementHub<TDbContext>>(finalPattern);
    }

    #region Legacy Methods (for backward compatibility)

    /// <summary>
    /// Adds SignalR User Management services.
    /// Consider using AddSignalRUserManagement instead for simpler configuration.
    /// </summary>
    /// <typeparam name="TDbContext">DbContext with UserConnections DbSet that implements IUserConnectionDbContext</typeparam>
    [Obsolete("Use AddSignalRUserManagement<TDbContext>() instead for simpler configuration.")]
    public static IServiceCollection AddUserManagement<TDbContext>(
        this IServiceCollection services,
        Action<UserManagementOptions>? configure = null)
        where TDbContext : DbContext, IUserConnectionDbContext
    {
        var options = new UserManagementOptions();
        configure?.Invoke(options);

        services.Configure<UserManagementOptions>(opt =>
        {
            opt.KeepAliveIntervalSeconds = options.KeepAliveIntervalSeconds;
            opt.ClientTimeoutSeconds = options.ClientTimeoutSeconds;
            opt.ReconnectDelaysSeconds = options.ReconnectDelaysSeconds;
        });

        services.AddSignalR(signalr =>
        {
            signalr.KeepAliveInterval = TimeSpan.FromSeconds(options.KeepAliveIntervalSeconds);
            signalr.ClientTimeoutInterval = TimeSpan.FromSeconds(options.ClientTimeoutSeconds);
        });

        services.AddScoped<IUserConnectionService, UserConnectionService<UserManagementHub<TDbContext>, TDbContext>>();
        services.TryAddScoped<IUserConnectionDbContext>(sp => sp.GetRequiredService<TDbContext>());

        return services;
    }

    /// <summary>
    /// Maps the UserManagementHub to the specified path.
    /// Consider using MapSignalRUserManagement instead.
    /// </summary>
    [Obsolete("Use MapSignalRUserManagement<TDbContext>() instead.")]
    public static HubEndpointConventionBuilder MapUserManagementHub<TDbContext>(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/hubs/usermanagement")
        where TDbContext : DbContext
    {
        return endpoints.MapHub<UserManagementHub<TDbContext>>(pattern);
    }

    /// <summary>
    /// Adds method invocation (communicator) functionality.
    /// Consider using AddSignalRUserManagement with EnableCommunicator = true instead.
    /// </summary>
    [Obsolete("Use AddSignalRUserManagement<TDbContext>(opt => opt.EnableCommunicator = true) instead.")]
    public static IServiceCollection AddUserManagementCommunicator<TDbContext>(
        this IServiceCollection services,
        Action<CommunicatorOptions>? configure = null)
        where TDbContext : DbContext, IUserConnectionDbContext
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

        services.TryAddSingleton<IResponseManager, ResponseManager>();
        services.AddScoped<IUserCommunicatorService, UserCommunicatorService<UserManagementHub<TDbContext>>>();

        return services;
    }

    #endregion
}
