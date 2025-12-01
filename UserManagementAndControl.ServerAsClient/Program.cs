using Nesco.SignalRUserManagement.Client.Extensions;
using UserManagementAndControl.ServerAsClient.Components;
using UserManagementAndControl.ServerAsClient.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// SignalR User Management Client - Blazor Server as Client
// =============================================================================
//
// This Blazor Server app acts as a client to the main UserManagement server.
// It connects to the server's SignalR hub and can receive method invocations.
// =============================================================================

// Server API base URL
const string serverUrl = "http://localhost:5000";

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add logging
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configure HttpClient to call the server API
builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri(serverUrl);
});

// Register a default HttpClient for services that need it (like DefaultFileUploadService)
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("ServerApi");
});

// Add authentication service as singleton to share state across the app
// Uses IHttpClientFactory internally to create HttpClient instances
builder.Services.AddSingleton<AuthService>();

// Register the method invocation logger as a singleton for tracking method calls
builder.Services.AddSingleton<MethodInvocationLogger>();

// ============================================================================
// SignalR User Management Client - Single call to add ALL services
// ============================================================================
builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
{
    options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
    options.EnableFileUpload = true; // Enable file upload for large responses (like GetLargeData)
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Comment out HTTPS redirection for HTTP development
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();