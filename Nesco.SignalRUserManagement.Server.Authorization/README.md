# Nesco.SignalRUserManagement.Server.Authorization

Server-side JWT authentication library for ASP.NET Core applications using SignalR User Management.

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Server.Authorization
```

## Setup

### 1. Create Your AuthController (Required)

The library provides a generic `AuthController<TUser>` base class. **You must create a concrete controller** in your application that inherits from it:

```csharp
using Microsoft.AspNetCore.Identity;
using Nesco.SignalRUserManagement.Server.Authorization.Controllers;

namespace YourApp.Controllers;

public class AuthController : AuthController<ApplicationUser>
{
    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
        : base(signInManager, userManager, configuration)
    {
    }
}
```

> **Why is this required?** ASP.NET Core's controller discovery doesn't automatically register generic controllers. By creating a concrete class, you also get the flexibility to use your own user type (e.g., `ApplicationUser` with custom properties).

### 2. Add the SignalR UserIdProvider

```csharp
using Nesco.SignalRUserManagement.Server.Authorization.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR UserIdProvider for JWT authentication
builder.Services.AddSignalRUserIdProvider();
```

### 3. Configure JWT Authentication

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Get JWT settings from configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "UserManagementAndControl";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "UserManagementAndControl";

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Allow JWT token from query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

### 4. Add Controllers

```csharp
builder.Services.AddControllers();

// ...

var app = builder.Build();

// ...

app.MapControllers();
```

### 5. Configure appsettings.json (Recommended)

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "YourAppName",
    "Audience": "YourAppName"
  }
}
```

> **Important:** The `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` values must match between the server's JWT validation and token generation. If not configured, both default to `"UserManagementAndControl"`.

## Complete Example

```csharp
// Program.cs
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Nesco.SignalRUserManagement.Server.Authorization.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Identity
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "UserManagementAndControl";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "UserManagementAndControl";

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Add SignalR UserIdProvider
builder.Services.AddSignalRUserIdProvider();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

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

### SignalRUserIdProvider

Extracts the user ID from JWT claims for SignalR connections. It checks these claims in order:
1. `ClaimTypes.NameIdentifier`
2. `ClaimTypes.Name`
3. `"sub"`

### Extension Methods

| Method | Description |
|--------|-------------|
| `AddSignalRUserIdProvider()` | Registers `IUserIdProvider` for SignalR JWT authentication |

## Features

- Generic `AuthController<TUser>` works with any `IdentityUser` subclass
- JWT token generation with configurable expiration (7 days default)
- SignalR-compatible `IUserIdProvider` for user identification
- Configurable JWT settings via `appsettings.json`

## Companion Library

Use with `Nesco.SignalRUserManagement.Client.Authorization` for a complete client-side authentication solution.
