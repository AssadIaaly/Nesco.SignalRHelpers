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

    /// <summary>
    /// The username associated with this connection (resolved at connection time)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Extra data that can be stored with the connection (e.g., JSON metadata)
    /// </summary>
    public string? Extra { get; set; }

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}
