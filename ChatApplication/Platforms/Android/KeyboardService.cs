using Android.App;
using Android.Views;
using ChatApplication.Services;
using AndroidView = Android.Views.View;
using AndroidRect = Android.Graphics.Rect;

namespace ChatApplication.Platforms.Android;

public class KeyboardService : IKeyboardService
{
    private Activity? _activity;
    private GlobalLayoutListener? _layoutListener;
    private AndroidView? _rootView;
    private int _previousKeyboardHeight;

    public event Action<double>? KeyboardHeightChanged;
    public double CurrentKeyboardHeight { get; private set; }
    public bool IsKeyboardVisible => CurrentKeyboardHeight > 0;

    public void Initialize()
    {
        _activity = Platform.CurrentActivity;
        if (_activity == null)
            return;

        _rootView = _activity.FindViewById(global::Android.Resource.Id.Content);
        if (_rootView == null)
            return;

        _layoutListener = new GlobalLayoutListener(this, _rootView, _activity);
        _rootView.ViewTreeObserver?.AddOnGlobalLayoutListener(_layoutListener);
    }

    internal void OnKeyboardHeightChanged(int heightPx)
    {
        if (heightPx == _previousKeyboardHeight)
            return;

        _previousKeyboardHeight = heightPx;

        // Convert pixels to device-independent units
        var density = _activity?.Resources?.DisplayMetrics?.Density ?? 1;
        CurrentKeyboardHeight = heightPx / density;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            KeyboardHeightChanged?.Invoke(CurrentKeyboardHeight);
        });
    }

    private class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly KeyboardService _service;
        private readonly AndroidView _rootView;
        private readonly Activity _activity;

        public GlobalLayoutListener(KeyboardService service, AndroidView rootView, Activity activity)
        {
            _service = service;
            _rootView = rootView;
            _activity = activity;
        }

        public void OnGlobalLayout()
        {
            var rect = new AndroidRect();
            _rootView.GetWindowVisibleDisplayFrame(rect);

            var screenHeight = _rootView.RootView?.Height ?? 0;
            var keyboardHeight = screenHeight - rect.Bottom;

            // Account for navigation bar height
            var navigationBarHeight = GetNavigationBarHeight();
            keyboardHeight = Math.Max(0, keyboardHeight - navigationBarHeight);

            // Only consider it a keyboard if height is significant (> 150px)
            if (keyboardHeight < 150)
                keyboardHeight = 0;

            _service.OnKeyboardHeightChanged(keyboardHeight);
        }

        private int GetNavigationBarHeight()
        {
            var resources = _activity.Resources;
            if (resources == null)
                return 0;

            var resourceId = resources.GetIdentifier("navigation_bar_height", "dimen", "android");
            if (resourceId > 0)
            {
                return resources.GetDimensionPixelSize(resourceId);
            }
            return 0;
        }
    }
}
