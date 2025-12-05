using ChatApplication.Server.Data;
using ChatApplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Server.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConversationService _conversationService;

    public ChatService(ApplicationDbContext dbContext, IConversationService conversationService)
    {
        _dbContext = dbContext;
        _conversationService = conversationService;
    }

    public async Task<ChatMessage> SendMessageAsync(string senderId, string receiverId, string text)
    {
        var conversation = await _conversationService.GetOrCreateConversationAsync(senderId, receiverId);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            ConversationId = conversation.Id,
            Text = text,
            SentAt = DateTime.UtcNow,
            Status = MessageStatus.Sent
        };

        _dbContext.ChatMessages.Add(message);

        // Update conversation with last message info
        var preview = text.Length > 50 ? text[..50] + "..." : text;
        await _conversationService.UpdateLastMessageAsync(conversation.Id, preview, message.SentAt);

        await _dbContext.SaveChangesAsync();

        return message;
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(string conversationId, string currentUserId, int skip = 0, int take = 50)
    {
        var messages = await _dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Select(m => new ChatMessageDto(
                m.Id,
                m.SenderId,
                m.ReceiverId,
                m.ConversationId,
                m.Text,
                m.SentAt,
                m.DeliveredAt,
                m.ReadAt,
                m.Status,
                m.SenderId == currentUserId
            ))
            .ToListAsync();

        // Return in chronological order for display
        messages.Reverse();
        return messages;
    }

    public async Task<ChatMessage?> GetMessageAsync(Guid messageId)
    {
        return await _dbContext.ChatMessages.FindAsync(messageId);
    }

    public async Task MarkMessageAsDeliveredAsync(Guid messageId)
    {
        var message = await _dbContext.ChatMessages.FindAsync(messageId);
        if (message != null && message.Status == MessageStatus.Sent)
        {
            message.DeliveredAt = DateTime.UtcNow;
            message.Status = MessageStatus.Delivered;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<Guid>> MarkConversationAsReadAsync(string conversationId, string userId)
    {
        var now = DateTime.UtcNow;

        // Get all unread messages sent to this user in this conversation
        var unreadMessages = await _dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId
                        && m.ReceiverId == userId
                        && m.Status != MessageStatus.Read)
            .ToListAsync();

        var messageIds = new List<Guid>();

        foreach (var message in unreadMessages)
        {
            message.ReadAt = now;
            message.Status = MessageStatus.Read;
            if (message.DeliveredAt == null)
            {
                message.DeliveredAt = now;
            }
            messageIds.Add(message.Id);
        }

        if (unreadMessages.Any())
        {
            await _dbContext.SaveChangesAsync();
        }

        return messageIds;
    }

    public async Task<int> GetUnreadCountAsync(string conversationId, string userId)
    {
        return await _dbContext.ChatMessages
            .CountAsync(m => m.ConversationId == conversationId
                             && m.ReceiverId == userId
                             && m.Status != MessageStatus.Read);
    }
}
