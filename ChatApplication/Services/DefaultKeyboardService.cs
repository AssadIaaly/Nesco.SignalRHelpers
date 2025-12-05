namespace ChatApplication.Services;

public class DefaultKeyboardService : IKeyboardService
{
    public event Action<double>? KeyboardHeightChanged;
    public double CurrentKeyboardHeight => 0;
    public bool IsKeyboardVisible => false;

    public void Initialize()
    {
        // No-op for non-mobile platforms
    }
}
