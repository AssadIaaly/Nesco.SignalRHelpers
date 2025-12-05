using ChatApplication.Server.Models;

namespace ChatApplication.Server.Services;

public interface IChatService
{
    Task<ChatMessage> SendMessageAsync(string senderId, string receiverId, string text);
    Task<List<ChatMessageDto>> GetMessagesAsync(string conversationId, string currentUserId, int skip = 0, int take = 50);
    Task<ChatMessage?> GetMessageAsync(Guid messageId);
    Task MarkMessageAsDeliveredAsync(Guid messageId);
    Task<List<Guid>> MarkConversationAsReadAsync(string conversationId, string userId);
    Task<int> GetUnreadCountAsync(string conversationId, string userId);
}
