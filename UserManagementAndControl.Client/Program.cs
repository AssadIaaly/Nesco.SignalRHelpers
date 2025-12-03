using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;
using Nesco.SignalRUserManagement.Client.Authorization.Services;
using Nesco.SignalRUserManagement.Client.Extensions;
using UserManagementAndControl.Client;
using UserManagementAndControl.Client.Services;

// =============================================================================
// SignalR User Management Client - Simplified API
// =============================================================================
//
// ONE call to add all services: AddSignalRUserManagementClient<TMethodExecutor>()
//
// The library includes:
// - UserConnectionClient for SignalR connection management
// - Automatic reconnection with configurable delays
// - Method invocation handling via IMethodExecutor
// =============================================================================

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Server API base URL
const string serverUrl = "http://localhost:5000";

// Configure HttpClient to call the server API
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(serverUrl)
});

// Add logging
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add authentication services from the authorization library
builder.Services.AddSignalRClientAuth();

// Register the method invocation logger as a singleton for tracking method calls
builder.Services.AddSingleton<MethodInvocationLogger>();

// ============================================================================
// SignalR User Management Client - Single call to add ALL services
// ============================================================================
builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
{
    options.MaxDirectDataSizeBytes = 10 * 1024; // 64KB
});

await builder.Build().RunAsync();
