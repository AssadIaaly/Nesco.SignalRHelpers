using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Nesco.SignalRUserManagement.Client.Handlers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure logging for debugging
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddFilter("Nesco.SignalRUserManagement", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// ============================================================================
// SignalR User Management Client with Reflection-based Handler Discovery
// ============================================================================
// Handlers implementing ISignalRHandler are auto-discovered from the entry assembly.
// Method names in handler classes = SignalR method names the server can invoke.
// See: Services/AppSignalRHandlers.cs for available methods
// ============================================================================
builder.Services.AddSignalRUserManagementClientWithHandlers(options =>
{
    // Configure HubUrl so components don't need to specify it
    options.HubUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/') + "/hubs/usermanagement";
});

await builder.Build().RunAsync();