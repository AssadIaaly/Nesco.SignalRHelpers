using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;
using Nesco.SignalRUserManagement.Client.Handlers;
using UserManagementAndControl.Client.Reflection;

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

// Add authentication services
builder.Services.AddSignalRClientAuth();

// ============================================================================
// SignalR User Management Client with Reflection-based Handler Discovery
// ============================================================================
// This is all you need! Handlers implementing ISignalRHandler are auto-discovered
// from the entry assembly by default. Method names = SignalR method names.
// ============================================================================

// Simplest usage - scans entry assembly automatically:
builder.Services.AddSignalRUserManagementClientWithHandlers();

// With client options:
// builder.Services.AddSignalRUserManagementClientWithHandlers(
//     client => client.MaxDirectDataSizeBytes = 10 * 1024  // 10KB before file upload
// );

await builder.Build().RunAsync();
