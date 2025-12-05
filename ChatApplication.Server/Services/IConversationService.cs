using ChatApplication.Server.Models;

namespace ChatApplication.Server.Services;

public interface IConversationService
{
    Task<Conversation> GetOrCreateConversationAsync(string user1Id, string user2Id);
    Task<Conversation?> GetConversationAsync(string conversationId);
    Task<List<ConversationDto>> GetUserConversationsAsync(string userId);
    Task UpdateLastMessageAsync(string conversationId, string preview, DateTime timestamp);
}
