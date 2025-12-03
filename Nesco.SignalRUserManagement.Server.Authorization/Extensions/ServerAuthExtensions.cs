using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nesco.SignalRUserManagement.Server.Authorization.Options;
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

    /// <summary>
    /// Adds JWT Bearer authentication configured for SignalR with query string token support.
    /// Also registers the SignalR UserIdProvider.
    /// </summary>
    /// <param name="builder">The authentication builder</param>
    /// <param name="configure">Action to configure JWT options</param>
    /// <returns>The authentication builder for chaining</returns>
    public static AuthenticationBuilder AddSignalRJwtBearer(
        this AuthenticationBuilder builder,
        Action<JwtAuthOptions> configure)
    {
        var options = new JwtAuthOptions();
        configure(options);

        return builder.AddSignalRJwtBearerInternal(options);
    }

    /// <summary>
    /// Adds JWT Bearer authentication configured for SignalR with query string token support.
    /// Reads configuration from the "Jwt" section of IConfiguration.
    /// Also registers the SignalR UserIdProvider.
    /// </summary>
    /// <param name="builder">The authentication builder</param>
    /// <param name="configuration">The configuration to read JWT settings from</param>
    /// <returns>The authentication builder for chaining</returns>
    public static AuthenticationBuilder AddSignalRJwtBearer(
        this AuthenticationBuilder builder,
        IConfiguration configuration)
    {
        var options = new JwtAuthOptions();

        var jwtSection = configuration.GetSection("Jwt");
        if (jwtSection.Exists())
        {
            options.Key = jwtSection["Key"] ?? options.Key;
            options.Issuer = jwtSection["Issuer"] ?? options.Issuer;
            options.Audience = jwtSection["Audience"] ?? options.Audience;

            if (int.TryParse(jwtSection["TokenExpirationDays"], out var expDays))
                options.TokenExpirationDays = expDays;

            if (jwtSection["HubPathPrefix"] is { } hubPath)
                options.HubPathPrefix = hubPath;
        }

        return builder.AddSignalRJwtBearerInternal(options);
    }

    /// <summary>
    /// Adds JWT Bearer authentication configured for SignalR with query string token support.
    /// Uses default options. Also registers the SignalR UserIdProvider.
    /// </summary>
    /// <param name="builder">The authentication builder</param>
    /// <returns>The authentication builder for chaining</returns>
    public static AuthenticationBuilder AddSignalRJwtBearer(this AuthenticationBuilder builder)
    {
        return builder.AddSignalRJwtBearerInternal(new JwtAuthOptions());
    }

    private static AuthenticationBuilder AddSignalRJwtBearerInternal(
        this AuthenticationBuilder builder,
        JwtAuthOptions options)
    {
        // Register the UserIdProvider
        builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

        // Store options for use by AuthController
        builder.Services.Configure<JwtAuthOptions>(opt =>
        {
            opt.Key = options.Key;
            opt.Issuer = options.Issuer;
            opt.Audience = options.Audience;
            opt.HubPathPrefix = options.HubPathPrefix;
            opt.TokenExpirationDays = options.TokenExpirationDays;
            opt.ValidateIssuer = options.ValidateIssuer;
            opt.ValidateAudience = options.ValidateAudience;
            opt.ValidateLifetime = options.ValidateLifetime;
        });

        builder.AddJwtBearer(jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = options.ValidateIssuer,
                ValidateAudience = options.ValidateAudience,
                ValidateLifetime = options.ValidateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key))
            };

            // Allow JWT token from query string for SignalR
            jwtOptions.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(options.HubPathPrefix))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return builder;
    }
}
