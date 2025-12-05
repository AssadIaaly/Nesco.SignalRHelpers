using ChatApplication.Services;

namespace ChatApplication;

public partial class MainPage : ContentPage
{
    public MainPage(IKeyboardService keyboardService)
    {
        InitializeComponent();

        // Initialize keyboard service for soft keyboard detection
        keyboardService.Initialize();
    }
}