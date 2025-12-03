using Microsoft.AspNetCore.Components.Authorization;

namespace Nesco.SignalRUserManagement.Client.Authorization.Services;

public class SignalRAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;
    private bool _hasCheckedAuth;

    public SignalRAuthStateProvider(AuthService authService)
    {
        _authService = authService;
        _authService.AuthStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged()
    {
        // Reset check flag when auth state changes (e.g., after login/logout)
        _hasCheckedAuth = true;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Only check server once on initial load
        if (!_hasCheckedAuth)
        {
            _hasCheckedAuth = true;
            await _authService.CheckAuthAsync();
        }

        return new AuthenticationState(_authService.CurrentUser);
    }
}
