using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Client.Authorization.Extensions;
using Nesco.SignalRUserManagement.Client.Extensions;
using UserManagementAndControl.MAUI.Services;

namespace UserManagementAndControl.MAUI;

public static class MauiProgram
{
    // ==========================================================================
    // Server URL configuration
    // ==========================================================================
    // Set this to your server's address. Examples:
    // - "http://localhost:5000"        - For Windows/Mac desktop
    // - "http://192.168.1.100:5000"    - For mobile devices (use server's IP)
    //
    // To find your server's IP on Windows: Run "ipconfig" in cmd
    // Make sure mobile devices are on the same network as the server
    // ==========================================================================
    private const string ServerUrl = "http://192.168.1.5:5000";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        // Configure HttpClient for server API
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(ServerUrl)
        });

        // Add authentication services with MAUI Preferences storage
        builder.Services.AddSignalRClientAuth<PreferencesAuthTokenStorage>();

        // Add method invocation logger
        builder.Services.AddSingleton<MethodInvocationLogger>();

        // SignalR User Management Client - single call to add all services
        builder.Services.AddSignalRUserManagementClient<ClientMethodExecutor>(options =>
        {
            options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
            options.EnableFileUpload = true; // Enable file upload for large responses (like GetLargeData)
        });

        return builder.Build();
    }
}
