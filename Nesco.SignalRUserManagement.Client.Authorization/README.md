# Nesco.SignalRUserManagement.Client.Authorization

Client-side JWT authentication library for Blazor WebAssembly and MAUI applications using SignalR User Management.

## Installation

```bash
dotnet add package Nesco.SignalRUserManagement.Client.Authorization
```

## Setup

### 1. Configure HttpClient

Register an `HttpClient` with the base address pointing to your server **before** adding auth services:

```csharp
// Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient with your server URL
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000")
});
```

### 2. Add Auth Services

#### Blazor WebAssembly (Default - uses localStorage)

```csharp
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;

// Add authentication services (uses localStorage by default)
builder.Services.AddSignalRClientAuth();
```

#### Blazor Server (uses ProtectedSessionStorage)

For Blazor Server apps, use encrypted session storage:

```csharp
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;

// Add authentication services with protected session storage
builder.Services.AddSignalRClientAuth<ProtectedSessionAuthTokenStorage>();
```

You'll need to implement `ProtectedSessionAuthTokenStorage`:

```csharp
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Nesco.SignalRUserManagement.Client.Authorization.Services;

public class ProtectedSessionAuthTokenStorage : IAuthTokenStorage
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private const string TokenKey = "SignalRAuth.Token";
    private const string UserIdKey = "SignalRAuth.UserId";
    private const string EmailKey = "SignalRAuth.Email";

    public ProtectedSessionAuthTokenStorage(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(TokenKey);
            return result.Success ? result.Value : null;
        }
        catch { return null; }
    }

    public async Task<string?> GetUserIdAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(UserIdKey);
            return result.Success ? result.Value : null;
        }
        catch { return null; }
    }

    public async Task<string?> GetEmailAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>(EmailKey);
            return result.Success ? result.Value : null;
        }
        catch { return null; }
    }

    public async Task SaveAsync(string? token, string userId, string email)
    {
        try
        {
            if (!string.IsNullOrEmpty(token))
            {
                await _sessionStorage.SetAsync(TokenKey, token);
                await _sessionStorage.SetAsync(UserIdKey, userId);
                await _sessionStorage.SetAsync(EmailKey, email);
            }
        }
        catch { }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _sessionStorage.DeleteAsync(TokenKey);
            await _sessionStorage.DeleteAsync(UserIdKey);
            await _sessionStorage.DeleteAsync(EmailKey);
        }
        catch { }
    }
}
```

#### MAUI or Custom Storage

For MAUI apps or other platforms, implement `IAuthTokenStorage` and register it:

```csharp
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;

// Add authentication services with custom storage
builder.Services.AddSignalRClientAuth<PreferencesAuthTokenStorage>();
```

### 3. Configure App.razor

Wrap your app with `CascadingAuthenticationState`:

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

## Custom Token Storage

The library uses an `IAuthTokenStorage` interface for persisting authentication data. This allows you to implement custom storage mechanisms for different platforms.

### IAuthTokenStorage Interface

```csharp
public interface IAuthTokenStorage
{
    Task<string?> GetTokenAsync();
    Task<string?> GetUserIdAsync();
    Task<string?> GetEmailAsync();
    Task SaveAsync(string? token, string userId, string email);
    Task ClearAsync();
}
```

### Built-in Implementation

- **`LocalStorageAuthTokenStorage`** - Default for Blazor WebAssembly, uses browser localStorage via JSInterop
- **`ProtectedSessionAuthTokenStorage`** - For Blazor Server, uses encrypted session storage (see example above)

### MAUI Example (using Preferences)

```csharp
using Nesco.SignalRUserManagement.Client.Authorization.Services;

public class PreferencesAuthTokenStorage : IAuthTokenStorage
{
    private const string TokenKey = "authToken";
    private const string UserIdKey = "userId";
    private const string EmailKey = "userEmail";

    public Task<string?> GetTokenAsync() =>
        Task.FromResult(Preferences.Default.Get<string?>(TokenKey, null));

    public Task<string?> GetUserIdAsync() =>
        Task.FromResult(Preferences.Default.Get<string?>(UserIdKey, null));

    public Task<string?> GetEmailAsync() =>
        Task.FromResult(Preferences.Default.Get<string?>(EmailKey, null));

    public Task SaveAsync(string? token, string userId, string email)
    {
        Preferences.Default.Set(TokenKey, token ?? "");
        Preferences.Default.Set(UserIdKey, userId);
        Preferences.Default.Set(EmailKey, email);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Default.Remove(TokenKey);
        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(EmailKey);
        return Task.CompletedTask;
    }
}
```

Then register it in `MauiProgram.cs`:

```csharp
builder.Services.AddSignalRClientAuth<PreferencesAuthTokenStorage>();
```

### SecureStorage Example (for sensitive data)

```csharp
public class SecureStorageAuthTokenStorage : IAuthTokenStorage
{
    private const string TokenKey = "authToken";
    private const string UserIdKey = "userId";
    private const string EmailKey = "userEmail";

    public async Task<string?> GetTokenAsync() =>
        await SecureStorage.Default.GetAsync(TokenKey);

    public async Task<string?> GetUserIdAsync() =>
        await SecureStorage.Default.GetAsync(UserIdKey);

    public async Task<string?> GetEmailAsync() =>
        await SecureStorage.Default.GetAsync(EmailKey);

    public async Task SaveAsync(string? token, string userId, string email)
    {
        if (!string.IsNullOrEmpty(token))
            await SecureStorage.Default.SetAsync(TokenKey, token);
        await SecureStorage.Default.SetAsync(UserIdKey, userId);
        await SecureStorage.Default.SetAsync(EmailKey, email);
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(EmailKey);
        return Task.CompletedTask;
    }
}
```

## Usage

### Login Page

```razor
@page "/login"
@using Nesco.SignalRUserManagement.Client.Authorization.Services
@inject AuthService AuthService
@inject NavigationManager Navigation

<EditForm Model="loginModel" OnValidSubmit="HandleLogin">
    <div>
        <label>Email</label>
        <InputText @bind-Value="loginModel.Email" />
    </div>
    <div>
        <label>Password</label>
        <InputText @bind-Value="loginModel.Password" type="password" />
    </div>
    <button type="submit">Login</button>
</EditForm>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}

@code {
    private LoginModel loginModel = new();
    private string? errorMessage;

    private async Task HandleLogin()
    {
        var result = await AuthService.LoginAsync(loginModel.Email, loginModel.Password);

        if (result.Success)
        {
            Navigation.NavigateTo("/");
        }
        else
        {
            errorMessage = result.Error ?? "Login failed";
        }
    }

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
```

### Protected Pages

```razor
@page "/"
@using Nesco.SignalRUserManagement.Client.Authorization.Services
@inject AuthService AuthService
@inject NavigationManager Navigation

@if (!AuthService.IsAuthenticated)
{
    <p>Redirecting to login...</p>
}
else
{
    <h1>Welcome, @AuthService.CurrentUser.Identity?.Name</h1>
    <button @onclick="HandleLogout">Logout</button>
}

@code {
    protected override async Task OnInitializedAsync()
    {
        await AuthService.CheckAuthAsync();

        if (!AuthService.IsAuthenticated)
        {
            Navigation.NavigateTo("/login");
        }
    }

    private async Task HandleLogout()
    {
        await AuthService.LogoutAsync();
        Navigation.NavigateTo("/login");
    }
}
```

### Using with SignalR

Pass the access token to your SignalR connection:

```csharp
await connectionClient.StartAsync(
    "http://localhost:5000/hubs/usermanagement",
    () => Task.FromResult(AuthService.AccessToken!));
```

## API Reference

### AuthService

| Property | Type | Description |
|----------|------|-------------|
| `CurrentUser` | `ClaimsPrincipal` | The current authenticated user |
| `IsAuthenticated` | `bool` | Whether the user is authenticated |
| `AccessToken` | `string?` | The JWT access token |
| `UserId` | `string?` | The current user's ID |

| Method | Returns | Description |
|--------|---------|-------------|
| `LoginAsync(email, password)` | `Task<LoginResult>` | Authenticate with email and password |
| `LogoutAsync()` | `Task` | Clear authentication state |
| `CheckAuthAsync()` | `Task<bool>` | Initialize from storage and check auth status |

| Event | Description |
|-------|-------------|
| `AuthStateChanged` | Raised when authentication state changes |

### LoginResult

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether login succeeded |
| `Error` | `string?` | Error message if login failed |

### IAuthTokenStorage

| Method | Returns | Description |
|--------|---------|-------------|
| `GetTokenAsync()` | `Task<string?>` | Get stored token |
| `GetUserIdAsync()` | `Task<string?>` | Get stored user ID |
| `GetEmailAsync()` | `Task<string?>` | Get stored email |
| `SaveAsync(token, userId, email)` | `Task` | Save auth data |
| `ClearAsync()` | `Task` | Clear all auth data |

## Features

- JWT token storage with pluggable storage backends
- Default localStorage support for Blazor WebAssembly
- Easy to implement custom storage for MAUI (Preferences, SecureStorage)
- Automatic token restoration on app startup
- Integration with Blazor's `AuthenticationStateProvider`
- Works with `AuthorizeView` and `[Authorize]` attributes

## Server Requirements

This library expects your server to have an auth endpoint at `api/auth/login` that:

- Accepts POST requests with JSON body: `{ "email": "...", "password": "..." }`
- Returns JSON on success: `{ "userId": "...", "email": "...", "token": "..." }`

Use the companion library `Nesco.SignalRUserManagement.Server.Authorization` for a ready-made implementation.
