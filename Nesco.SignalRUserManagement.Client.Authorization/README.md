# Nesco.SignalRUserManagement.Client.Authorization

Client-side JWT authentication library for Blazor WebAssembly applications using SignalR User Management.

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

```csharp
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;

// Add authentication services
builder.Services.AddSignalRClientAuth();
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
| `CheckAuthAsync()` | `Task<bool>` | Initialize from localStorage and check auth status |

| Event | Description |
|-------|-------------|
| `AuthStateChanged` | Raised when authentication state changes |

### LoginResult

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether login succeeded |
| `Error` | `string?` | Error message if login failed |

## Features

- JWT token storage in localStorage (persists across page reloads)
- Automatic token restoration on app startup
- Integration with Blazor's `AuthenticationStateProvider`
- Works with `AuthorizeView` and `[Authorize]` attributes

## Server Requirements

This library expects your server to have an auth endpoint at `api/auth/login` that:

- Accepts POST requests with JSON body: `{ "email": "...", "password": "..." }`
- Returns JSON on success: `{ "userId": "...", "email": "...", "token": "..." }`

Use the companion library `Nesco.SignalRUserManagement.Server.Authorization` for a ready-made implementation.
