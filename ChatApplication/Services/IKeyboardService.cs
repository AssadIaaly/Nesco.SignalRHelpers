namespace ChatApplication.Services;

public interface IKeyboardService
{
    event Action<double>? KeyboardHeightChanged;
    double CurrentKeyboardHeight { get; }
    bool IsKeyboardVisible { get; }
    void Initialize();
}
