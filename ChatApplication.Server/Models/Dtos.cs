namespace ChatApplication.Server.Models;

// User DTOs
public record UserDto(
    string Id,
    string Email,
    string? DisplayName,
    DateTime? LastSeen,
    bool IsOnline,
    string? ProfilePictureUrl
);

// Conversation DTOs
public record ConversationDto(
    string Id,
    UserDto OtherUser,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    int UnreadCount
);

// Message DTOs
public record ChatMessageDto(
    Guid Id,
    string SenderId,
    string ReceiverId,
    string ConversationId,
    string Text,
    DateTime SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    MessageStatus Status,
    bool IsMine
);

public record SendMessageRequest(
    string ReceiverId,
    string Text
);

public record SendMessageResponse(
    Guid MessageId,
    string ConversationId,
    DateTime SentAt
);

public record MarkAsReadRequest(
    string ConversationId,
    Guid? LastReadMessageId = null
);

public record TypingNotification(
    string UserId,
    string ConversationId,
    bool IsTyping
);

// Real-time message events
public record NewMessageEvent(
    Guid Id,
    string SenderId,
    string SenderDisplayName,
    string ConversationId,
    string Text,
    DateTime SentAt
);

public record MessageDeliveredEvent(
    Guid MessageId,
    string ConversationId,
    DateTime DeliveredAt
);

public record MessagesReadEvent(
    string ConversationId,
    string ReadByUserId,
    DateTime ReadAt,
    List<Guid>? MessageIds = null
);

public record UserStatusEvent(
    string UserId,
    bool IsOnline,
    DateTime? LastSeen,
    string? DisplayName = null
);

public record ForceLogoutEvent(
    string Reason
);
