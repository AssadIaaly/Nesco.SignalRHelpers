using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Nesco.SignalRUserManagement.Server.Authorization.Controllers;
using Nesco.SignalRUserManagement.Server.Authorization.Options;
using UserManagementAndControl.Server.Data;

namespace UserManagementAndControl.Server.Controllers;

public class AuthController : AuthController<ApplicationUser>
{
    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IOptions<JwtAuthOptions>? jwtOptions = null)
        : base(signInManager, userManager, configuration, jwtOptions)
    {
    }
}
