using Microsoft.AspNetCore.Identity;

namespace ChatApplication.Server.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTime? LastSeen { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
