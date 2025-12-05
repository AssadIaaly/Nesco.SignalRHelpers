namespace ChatApplication.Wasm.Models;

public enum MessageStatus
{
    Sent = 0,
    Delivered = 1,
    Read = 2
}

// User DTOs
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public string DisplayNameOrEmail => DisplayName ?? Email;

    public UserDto() { }
    public UserDto(string id, string email, string? displayName, DateTime? lastSeen, bool isOnline, string? profilePictureUrl)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        LastSeen = lastSeen;
        IsOnline = isOnline;
        ProfilePictureUrl = profilePictureUrl;
    }
}

// Conversation DTOs
public class ConversationDto
{
    public string Id { get; set; } = string.Empty;
    public UserDto OtherUser { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }

    public ConversationDto() { }
    public ConversationDto(string id, UserDto otherUser, DateTime createdAt, DateTime? lastMessageAt, string? lastMessagePreview, int unreadCount)
    {
        Id = id;
        OtherUser = otherUser;
        CreatedAt = createdAt;
        LastMessageAt = lastMessageAt;
        LastMessagePreview = lastMessagePreview;
        UnreadCount = unreadCount;
    }
}

// Message DTOs
public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public MessageStatus Status { get; set; }
    public bool IsMine { get; set; }

    public ChatMessageDto() { }
    public ChatMessageDto(Guid id, string senderId, string receiverId, string conversationId, string text, DateTime sentAt, DateTime? deliveredAt, DateTime? readAt, MessageStatus status, bool isMine)
    {
        Id = id;
        SenderId = senderId;
        ReceiverId = receiverId;
        ConversationId = conversationId;
        Text = text;
        SentAt = sentAt;
        DeliveredAt = deliveredAt;
        ReadAt = readAt;
        Status = status;
        IsMine = isMine;
    }
}

public class SendMessageRequest
{
    public string ReceiverId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public SendMessageRequest() { }
    public SendMessageRequest(string receiverId, string text)
    {
        ReceiverId = receiverId;
        Text = text;
    }
}

public class SendMessageResponse
{
    public Guid MessageId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    public SendMessageResponse() { }
    public SendMessageResponse(Guid messageId, string conversationId, DateTime sentAt)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        SentAt = sentAt;
    }
}

public class MarkAsReadRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public Guid? LastReadMessageId { get; set; }

    public MarkAsReadRequest() { }
    public MarkAsReadRequest(string conversationId, Guid? lastReadMessageId = null)
    {
        ConversationId = conversationId;
        LastReadMessageId = lastReadMessageId;
    }
}

public class TypingNotification
{
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public bool IsTyping { get; set; }

    public TypingNotification() { }
    public TypingNotification(string userId, string conversationId, bool isTyping)
    {
        UserId = userId;
        ConversationId = conversationId;
        IsTyping = isTyping;
    }
}

public class NewMessageEvent
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    public NewMessageEvent() { }
    public NewMessageEvent(Guid id, string senderId, string senderDisplayName, string conversationId, string text, DateTime sentAt)
    {
        Id = id;
        SenderId = senderId;
        SenderDisplayName = senderDisplayName;
        ConversationId = conversationId;
        Text = text;
        SentAt = sentAt;
    }
}

public class MessageDeliveredEvent
{
    public Guid MessageId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public DateTime DeliveredAt { get; set; }

    public MessageDeliveredEvent() { }
    public MessageDeliveredEvent(Guid messageId, string conversationId, DateTime deliveredAt)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        DeliveredAt = deliveredAt;
    }
}

public class MessagesReadEvent
{
    public string ConversationId { get; set; } = string.Empty;
    public string ReadByUserId { get; set; } = string.Empty;
    public DateTime ReadAt { get; set; }
    public List<Guid>? MessageIds { get; set; }

    public MessagesReadEvent() { }
    public MessagesReadEvent(string conversationId, string readByUserId, DateTime readAt, List<Guid>? messageIds = null)
    {
        ConversationId = conversationId;
        ReadByUserId = readByUserId;
        ReadAt = readAt;
        MessageIds = messageIds;
    }
}

public class UserStatusEvent
{
    public string UserId { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? DisplayName { get; set; }

    public UserStatusEvent() { }
    public UserStatusEvent(string userId, bool isOnline, DateTime? lastSeen, string? displayName = null)
    {
        UserId = userId;
        IsOnline = isOnline;
        LastSeen = lastSeen;
        DisplayName = displayName;
    }
}

public class ForceLogoutEvent
{
    public string Reason { get; set; } = string.Empty;

    public ForceLogoutEvent() { }
    public ForceLogoutEvent(string reason)
    {
        Reason = reason;
    }
}
