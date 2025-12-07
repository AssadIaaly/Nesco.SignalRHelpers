using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;
using Nesco.SignalRUserManagement.Client.Handlers;
using UserManagementAndControl.Client;
using UserManagementAndControl.Client.Services;

// =============================================================================
// SignalR User Management Client - Simplified API
// =============================================================================
//
// TWO approaches available:
//
// 1. OLD APPROACH: AddSignalRUserManagementClient<TMethodExecutor>()
//    - Requires implementing IMethodExecutor with a switch statement
//    - More boilerplate, but explicit control
//
// 2. NEW APPROACH: AddSignalRUserManagementClientWithHandlers()
//    - Auto-discovers handler classes implementing ISignalRHandler
//    - Method names match SignalR method names automatically
//    - Less boilerplate, similar to Wolverine.FX
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
// NEW APPROACH: Auto-discover SignalR handlers using reflection
// ============================================================================
// Handlers are classes that implement ISignalRHandler or have [SignalRHandler] attribute.
// Method names in the handler class match the SignalR method names.
// See SignalRHandlers/ClientSignalRHandlers.cs for the handler implementation.
// ============================================================================
builder.Services.AddSignalRUserManagementClientWithHandlers(
    handlers =>
    {
        // Scan the current assembly for handler classes
        handlers.AssembliesToScan.Add(typeof(Program).Assembly);
    },
    client =>
    {
        client.MaxDirectDataSizeBytes = 10 * 1024; // 10KB
    });

// ============================================================================
// OLD APPROACH: Explicit IMethodExecutor implementation (commented out)
// ============================================================================
// Uses a switch statement to route method calls - more boilerplate but explicit.
// See Services/ClientMethodExecutor.cs for the old implementation.
// ============================================================================
// using Nesco.SignalRUserManagement.Client.Extensions;
// builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
// {
//     options.MaxDirectDataSizeBytes = 10 * 1024; // 10KB
// });

await builder.Build().RunAsync();
