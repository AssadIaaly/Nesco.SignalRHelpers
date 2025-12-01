using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Client.Extensions;
using UserManagementAndControl.MAUI.Services;

namespace UserManagementAndControl.MAUI;

public static class MauiProgram
{
    // ==========================================================================
    // Server API base URL configuration
    // ==========================================================================
    // IMPORTANT: For Android physical devices, you MUST use your machine's
    // actual IP address (e.g., "http://192.168.1.100:5000")
    //
    // To find your IP:
    // - Windows: Run "ipconfig" in cmd, look for IPv4 Address
    // - Make sure your phone is on the same WiFi network as your dev machine
    // - Make sure the server is listening on 0.0.0.0 or the specific IP
    // ==========================================================================

    // TODO: Change this to your machine's IP for physical Android devices
    private const string DevMachineIp = "192.168.1.100"; // <-- UPDATE THIS!

#if ANDROID
    // For Android Emulator use 10.0.2.2, for physical device use DevMachineIp
    // Toggle between these as needed:
    private const string ServerUrl = "http://10.0.2.2:5000";           // Emulator
    // private const string ServerUrl = $"http://{DevMachineIp}:5000"; // Physical device
#else
    private const string ServerUrl = "http://localhost:5000";
#endif

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

        // Add authentication service
        builder.Services.AddSingleton<AuthService>();

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