using Microsoft.AspNetCore.Identity;
using Nesco.SignalRUserManagement.Server.Authorization.Controllers;
using UserManagementAndControl.Server.Data;

namespace UserManagementAndControl.Server.Controllers;

public class AuthController : AuthController<ApplicationUser>
{
    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
        : base(signInManager, userManager, configuration)
    {
    }
}
