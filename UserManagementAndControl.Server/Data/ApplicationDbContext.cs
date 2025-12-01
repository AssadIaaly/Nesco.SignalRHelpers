using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nesco.SignalRUserManagement.Server.Models;

namespace UserManagementAndControl.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IUserConnectionDbContext
{
    public DbSet<UserConnection> UserConnections { get; set; } = null!;
}