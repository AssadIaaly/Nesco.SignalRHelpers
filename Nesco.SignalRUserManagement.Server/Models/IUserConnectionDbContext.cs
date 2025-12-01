using Microsoft.EntityFrameworkCore;

namespace Nesco.SignalRUserManagement.Server.Models;

/// <summary>
/// Interface for DbContext that contains UserConnections
/// </summary>
public interface IUserConnectionDbContext
{
    DbSet<UserConnection> UserConnections { get; }
}
