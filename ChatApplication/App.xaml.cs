using ChatApplication.Services;

namespace ChatApplication;

public partial class App : Application
{
    private readonly IKeyboardService _keyboardService;

    public App(IKeyboardService keyboardService)
    {
        InitializeComponent();
        _keyboardService = keyboardService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage(_keyboardService)) { Title = "ChatApplication" };
    }
}