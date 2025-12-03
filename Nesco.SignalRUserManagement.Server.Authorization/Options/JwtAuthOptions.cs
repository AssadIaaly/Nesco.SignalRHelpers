namespace Nesco.SignalRUserManagement.Server.Authorization.Options;

/// <summary>
/// Options for configuring JWT authentication for SignalR
/// </summary>
public class JwtAuthOptions
{
    /// <summary>
    /// The secret key used to sign JWT tokens. Must be at least 32 characters.
    /// Default: "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
    /// </summary>
    public string Key { get; set; } = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

    /// <summary>
    /// The issuer of the JWT token.
    /// Default: "UserManagementAndControl"
    /// </summary>
    public string Issuer { get; set; } = "UserManagementAndControl";

    /// <summary>
    /// The audience of the JWT token.
    /// Default: "UserManagementAndControl"
    /// </summary>
    public string Audience { get; set; } = "UserManagementAndControl";

    /// <summary>
    /// The path prefix for SignalR hubs. Used to extract JWT tokens from query strings.
    /// Default: "/hubs"
    /// </summary>
    public string HubPathPrefix { get; set; } = "/hubs";

    /// <summary>
    /// Token expiration in days.
    /// Default: 7
    /// </summary>
    public int TokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Whether to validate the issuer.
    /// Default: true
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience.
    /// Default: true
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime.
    /// Default: true
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
}
