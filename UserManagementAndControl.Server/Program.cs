using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nesco.SignalRUserManagement.Server.Authorization.Extensions;
using Nesco.SignalRUserManagement.Server.Extensions;
using UserManagementAndControl.Server.Components;
using UserManagementAndControl.Server.Components.Account;
using UserManagementAndControl.Server.Data;
using UserManagementAndControl.Server.Hubs;

// =============================================================================
// SignalR User Management - Simplified API
// =============================================================================
//
// ONE call to add all services: AddSignalRUserManagement() or AddSignalRUserManagement<THub>()
// ONE call to map the hub: MapSignalRUserManagement() or MapSignalRUserManagement<THub>()
//
// The library includes:
// - User connection tracking (in-memory - no database required)
// - Method invocation on clients (communicator)
// - Dashboard component: <SignalRDashboard />
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Configure authentication with Identity cookies
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Add JWT Bearer authentication for API/SignalR
builder.Services.AddAuthentication()
    .AddSignalRJwtBearer(builder.Configuration);

builder.Services.AddAuthorization();

// Add CORS for external clients (WASM, ServerAsClient, and MAUI)
builder.Services.AddCors(options =>
{
    // Named policy for web clients with specific origins
    options.AddPolicy("WebClients", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5001",
                "https://localhost:5001",
                "http://localhost:5000",
                "https://localhost:7281",
                "http://localhost:5151",
                "http://localhost:5192",
                "https://localhost:5151")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    // Named policy for MAUI/Mobile clients
    // MAUI apps don't send a traditional Origin header, or may send various origins
    // depending on the platform (Android WebView, iOS WKWebView, Windows WebView2)
    options.AddPolicy("MobileClients", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin for mobile clients
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    // Default policy that combines both - allows all origins for development
    // In production, you may want to be more restrictive
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// ============================================================================
// SignalR User Management - Single call to add ALL services
// Using the <THub> overload since we have a custom AppHub
// No database required - uses in-memory connection tracking
// ============================================================================
builder.Services.AddSignalRUserManagement<AppHub>(options =>
{
    options.RequestTimeoutSeconds = 30;
    options.MaxConcurrentRequests = 20;
    options.AutoDeleteTempFiles = true;
});

// Add controllers for API endpoints
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Enable CORS - must be before authentication
app.UseCors();

// Comment out HTTPS redirection for HTTP development
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Map API controllers
app.MapControllers();

// ============================================================================
// Map the SignalR User Management Hub with custom AppHub
// AppHub extends UserManagementHub with additional methods like GetServerTime, Echo, etc.
// ============================================================================
app.MapSignalRUserManagement<AppHub>()
    .RequireAuthorization(policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, IdentityConstants.ApplicationScheme)
              .RequireAuthenticatedUser());

app.Run();
