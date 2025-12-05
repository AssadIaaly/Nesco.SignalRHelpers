using ChatApplication.Data;
using ChatApplication.Services;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Client.Extensions;

namespace ChatApplication;

public static class MauiProgram
{
    // Server URL configuration
    // For Android Emulator: use 10.0.2.2 (maps to host machine's localhost)
    // For Physical Android device: use your machine's IP address (e.g., 192.168.1.100)
    // For Windows/Mac/iOS Simulator: use localhost
    private const string DevMachineIp = "192.168.1.100"; // UPDATE THIS for physical device testing

#if ANDROID
    private const string ServerUrl = "http://10.0.2.2:5000"; // Android Emulator
    // private const string ServerUrl = $"http://{DevMachineIp}:5000"; // Physical device
#else
    private const string ServerUrl = "http://localhost:5000";
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // HTTP Client for API calls
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(ServerUrl)
        });

        // SQLite database for offline storage
        builder.Services.AddSingleton<ChatDatabase>();

        // Authentication service
        builder.Services.AddSingleton<AuthService>();

        // Chat service first (singleton to be shared across the app)
        // Must be registered before AddSignalRUserManagementClient since ChatMethodExecutor depends on it
        builder.Services.AddSingleton<ChatService>();

        // SignalR User Management Client - ChatMethodExecutor handles server-to-client method calls
        builder.Services.AddSignalRUserManagementClient<ChatMethodExecutor>(options =>
        {
            options.MaxDirectDataSizeBytes = 64 * 1024; // 64KB
            options.EnableFileUpload = true;
        });

        // Keyboard service for handling soft keyboard visibility
#if ANDROID
        builder.Services.AddSingleton<IKeyboardService, ChatApplication.Platforms.Android.KeyboardService>();
#else
        builder.Services.AddSingleton<IKeyboardService, DefaultKeyboardService>();
#endif

        return builder.Build();
    }
}
