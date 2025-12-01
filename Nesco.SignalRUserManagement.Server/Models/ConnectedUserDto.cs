namespace Nesco.SignalRUserManagement.Server.Models;

/// <summary>
/// DTO representing a connected user with their connections
/// </summary>
public class ConnectedUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public List<ConnectionDto> Connections { get; set; } = new();
}

/// <summary>
/// DTO representing a single connection
/// </summary>
public class ConnectionDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
}
