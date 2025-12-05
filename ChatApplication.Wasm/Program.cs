using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;
using Nesco.SignalRUserManagement.Client.Extensions;
using ChatApplication.Wasm;
using ChatApplication.Wasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Server API base URL - must match the ChatApplication.Server URL
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

// Add ChatService (scoped because HttpClient is scoped)
builder.Services.AddScoped<ChatService>();

// SignalR User Management Client - ChatMethodExecutor handles server-to-client method calls
builder.Services.AddSignalRUserManagementClient<ChatMethodExecutor>(options =>
{
    options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
});

await builder.Build().RunAsync();
