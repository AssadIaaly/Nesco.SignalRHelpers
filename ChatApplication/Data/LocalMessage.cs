using SQLite;

namespace ChatApplication.Data;

[Table("Messages")]
public class LocalMessage
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string ConversationId { get; set; } = string.Empty;

    public string SenderId { get; set; } = string.Empty;

    public string ReceiverId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    [Indexed]
    public DateTime SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public int Status { get; set; } // 0=Sent, 1=Delivered, 2=Read

    public bool IsMine { get; set; }
}

[Table("Conversations")]
public class LocalConversation
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string OtherUserId { get; set; } = string.Empty;

    public string OtherUserEmail { get; set; } = string.Empty;

    public string? OtherUserDisplayName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public string? LastMessagePreview { get; set; }

    public int UnreadCount { get; set; }
}

[Table("Users")]
public class LocalUser
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public DateTime? LastSeen { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
