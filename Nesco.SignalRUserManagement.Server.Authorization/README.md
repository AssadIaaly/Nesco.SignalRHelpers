# Nesco.SignalRUserManagement.Server.Authorization

Server-side JWT authentication library for ASP.NET Core applications using SignalR User Management.

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Server.Authorization
```

## Quick Setup

### 1. Create Your AuthController (Required)

The library provides a generic `AuthController<TUser>` base class. **You must create a concrete controller** in your application that inherits from it:

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Nesco.SignalRUserManagement.Server.Authorization.Controllers;
using Nesco.SignalRUserManagement.Server.Authorization.Options;

namespace YourApp.Controllers;

public class AuthController : AuthController<ApplicationUser>
{
    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IOptions<JwtAuthOptions>? jwtOptions = null)
        : base(signInManager, userManager, configuration, jwtOptions)
    {
    }
}
```

> **Why is this required?** ASP.NET Core's controller discovery doesn't automatically register generic controllers. By creating a concrete class, you also get the flexibility to use your own user type (e.g., `ApplicationUser` with custom properties).

### 2. Add JWT Authentication

```csharp
using Nesco.SignalRUserManagement.Server.Authorization.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Option A: Configure from appsettings.json
builder.Services.AddAuthentication()
    .AddSignalRJwtBearer(builder.Configuration);

// Option B: Configure inline
builder.Services.AddAuthentication()
    .AddSignalRJwtBearer(options =>
    {
        options.Key = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        options.Issuer = "YourAppName";
        options.Audience = "YourAppName";
    });

// Option C: Use defaults
builder.Services.AddAuthentication()
    .AddSignalRJwtBearer();
```

The `AddSignalRJwtBearer` extension automatically:
- Configures JWT Bearer authentication
- Sets up token validation parameters
- Enables query string token extraction for SignalR (`?access_token=...`)
- Registers the `IUserIdProvider` for SignalR

### 3. Add Controllers

```csharp
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

### 4. Configure appsettings.json (for Option A)

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "YourAppName",
    "Audience": "YourAppName",
    "TokenExpirationDays": 7,
    "HubPathPrefix": "/hubs"
  }
}
```

## Complete Example

```csharp
// Program.cs
using Microsoft.AspNetCore.Identity;
using Nesco.SignalRUserManagement.Server.Authorization.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Identity
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// Add JWT Authentication (reads from appsettings.json)
builder.Services.AddAuthentication()
    .AddSignalRJwtBearer(builder.Configuration);

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

## Combining with Cookie Authentication

If you need both cookie authentication (for Blazor Server) and JWT (for API/SignalR clients):

```csharp
// Cookie authentication for Blazor Server
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

// Add JWT Bearer for API/SignalR clients (separate call required)
builder.Services.AddAuthentication()
    .AddSignalRJwtBearer(builder.Configuration);
```

> **Note:** `AddIdentityCookies()` returns `IdentityCookiesBuilder`, not `AuthenticationBuilder`, so JWT must be added in a separate `AddAuthentication()` call.

Then configure your SignalR hub to accept both schemes:

```csharp
app.MapHub<YourHub>("/hubs/yourhub")
    .RequireAuthorization(policy =>
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme,
            IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser());
```

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Key` | `"YourSuperSecretKeyThatIsAtLeast32CharactersLong!"` | Secret key for signing tokens (min 32 chars) |
| `Issuer` | `"UserManagementAndControl"` | Token issuer |
| `Audience` | `"UserManagementAndControl"` | Token audience |
| `HubPathPrefix` | `"/hubs"` | Path prefix for SignalR query string token extraction |
| `TokenExpirationDays` | `7` | Token expiration in days |
| `ValidateIssuer` | `true` | Whether to validate issuer |
| `ValidateAudience` | `true` | Whether to validate audience |
| `ValidateLifetime` | `true` | Whether to validate token expiration |

## API Endpoints

The `AuthController<TUser>` provides these endpoints:

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | Authenticate and get JWT token | No |
| POST | `/api/auth/logout` | Logout (client-side token discard) | Yes |
| GET | `/api/auth/me` | Get current user info | Yes |

### Login Request

```json
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

### Login Response (Success)

```json
{
  "userId": "abc123",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

## API Reference

### Extension Methods

| Method | Description |
|--------|-------------|
| `AddSignalRJwtBearer()` | Adds JWT auth with default options |
| `AddSignalRJwtBearer(IConfiguration)` | Adds JWT auth from config's "Jwt" section |
| `AddSignalRJwtBearer(Action<JwtAuthOptions>)` | Adds JWT auth with inline configuration |
| `AddSignalRUserIdProvider()` | Registers only the `IUserIdProvider` (if configuring JWT manually) |

### SignalRUserIdProvider

Extracts the user ID from JWT claims for SignalR connections. It checks these claims in order:
1. `ClaimTypes.NameIdentifier`
2. `ClaimTypes.Name`
3. `"sub"`

## Companion Library

Use with `Nesco.SignalRUserManagement.Client.Authorization` for a complete client-side authentication solution.
