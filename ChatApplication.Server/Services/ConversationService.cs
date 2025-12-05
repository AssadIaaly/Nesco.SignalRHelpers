using ChatApplication.Server.Data;
using ChatApplication.Server.Models;
using Microsoft.EntityFrameworkCore;
using Nesco.SignalRUserManagement.Server.Services;

namespace ChatApplication.Server.Services;

public class ConversationService : IConversationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISignalRUserManagementService _userManagementService;

    public ConversationService(ApplicationDbContext dbContext, ISignalRUserManagementService userManagementService)
    {
        _dbContext = dbContext;
        _userManagementService = userManagementService;
    }

    public async Task<Conversation> GetOrCreateConversationAsync(string user1Id, string user2Id)
    {
        var conversationId = Conversation.GenerateId(user1Id, user2Id);

        var conversation = await _dbContext.Conversations.FindAsync(conversationId);

        if (conversation == null)
        {
            // Ensure user1Id is always the smaller one for consistency
            var ids = new[] { user1Id, user2Id }.OrderBy(x => x).ToArray();

            conversation = new Conversation
            {
                Id = conversationId,
                User1Id = ids[0],
                User2Id = ids[1],
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync();
        }

        return conversation;
    }

    public async Task<Conversation?> GetConversationAsync(string conversationId)
    {
        return await _dbContext.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId)
    {
        var conversations = await _dbContext.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var result = new List<ConversationDto>();

        foreach (var conv in conversations)
        {
            var otherUser = conv.User1Id == userId ? conv.User2! : conv.User1!;
            var isOnline = _userManagementService.IsUserConnected(otherUser.Id);

            var unreadCount = await _dbContext.ChatMessages
                .CountAsync(m => m.ConversationId == conv.Id
                                 && m.ReceiverId == userId
                                 && m.Status != MessageStatus.Read);

            var otherUserDto = new UserDto(
                otherUser.Id,
                otherUser.Email!,
                otherUser.DisplayName,
                otherUser.LastSeen,
                isOnline,
                otherUser.ProfilePictureUrl
            );

            result.Add(new ConversationDto(
                conv.Id,
                otherUserDto,
                conv.CreatedAt,
                conv.LastMessageAt,
                conv.LastMessagePreview,
                unreadCount
            ));
        }

        return result;
    }

    public async Task UpdateLastMessageAsync(string conversationId, string preview, DateTime timestamp)
    {
        var conversation = await _dbContext.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.LastMessagePreview = preview;
            conversation.LastMessageAt = timestamp;
        }
    }
}
