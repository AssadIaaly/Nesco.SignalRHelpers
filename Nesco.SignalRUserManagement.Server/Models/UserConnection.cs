using System.ComponentModel.DataAnnotations;

namespace Nesco.SignalRUserManagement.Server.Models;

/// <summary>
/// Database entity tracking a SignalR connection
/// </summary>
public class UserConnection
{
    [Key]
    public string ConnectionId { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}
