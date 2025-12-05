using SQLite;

namespace ChatApplication.Data;

public class ChatDatabase
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public ChatDatabase()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "chat.db");
    }

    private async Task InitAsync()
    {
        if (_database != null)
            return;

        _database = new SQLiteAsyncConnection(_dbPath);
        await _database.CreateTableAsync<LocalMessage>();
        await _database.CreateTableAsync<LocalConversation>();
        await _database.CreateTableAsync<LocalUser>();
    }

    // Messages
    public async Task<List<LocalMessage>> GetMessagesAsync(string conversationId, int skip = 0, int take = 50)
    {
        await InitAsync();
        return await _database!
            .Table<LocalMessage>()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task SaveMessageAsync(LocalMessage message)
    {
        await InitAsync();
        await _database!.InsertOrReplaceAsync(message);
    }

    public async Task SaveMessagesAsync(IEnumerable<LocalMessage> messages)
    {
        await InitAsync();
        foreach (var message in messages)
        {
            await _database!.InsertOrReplaceAsync(message);
        }
    }

    public async Task UpdateMessageStatusAsync(string messageId, int status, DateTime? deliveredAt = null, DateTime? readAt = null)
    {
        await InitAsync();
        var message = await _database!.FindAsync<LocalMessage>(messageId);
        if (message != null)
        {
            message.Status = status;
            if (deliveredAt.HasValue) message.DeliveredAt = deliveredAt.Value;
            if (readAt.HasValue) message.ReadAt = readAt.Value;
            await _database.UpdateAsync(message);
        }
    }

    // Conversations
    public async Task<List<LocalConversation>> GetConversationsAsync()
    {
        await InitAsync();
        return await _database!
            .Table<LocalConversation>()
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();
    }

    public async Task<LocalConversation?> GetConversationAsync(string conversationId)
    {
        await InitAsync();
        return await _database!.FindAsync<LocalConversation>(conversationId);
    }

    public async Task SaveConversationAsync(LocalConversation conversation)
    {
        await InitAsync();
        await _database!.InsertOrReplaceAsync(conversation);
    }

    public async Task SaveConversationsAsync(IEnumerable<LocalConversation> conversations)
    {
        await InitAsync();
        foreach (var conversation in conversations)
        {
            await _database!.InsertOrReplaceAsync(conversation);
        }
    }

    public async Task UpdateConversationLastMessageAsync(string conversationId, string preview, DateTime timestamp)
    {
        await InitAsync();
        var conversation = await _database!.FindAsync<LocalConversation>(conversationId);
        if (conversation != null)
        {
            conversation.LastMessagePreview = preview;
            conversation.LastMessageAt = timestamp;
            await _database.UpdateAsync(conversation);
        }
    }

    public async Task IncrementUnreadCountAsync(string conversationId)
    {
        await InitAsync();
        var conversation = await _database!.FindAsync<LocalConversation>(conversationId);
        if (conversation != null)
        {
            conversation.UnreadCount++;
            await _database.UpdateAsync(conversation);
        }
    }

    public async Task ClearUnreadCountAsync(string conversationId)
    {
        await InitAsync();
        var conversation = await _database!.FindAsync<LocalConversation>(conversationId);
        if (conversation != null)
        {
            conversation.UnreadCount = 0;
            await _database.UpdateAsync(conversation);
        }
    }

    // Users
    public async Task<List<LocalUser>> GetUsersAsync()
    {
        await InitAsync();
        return await _database!.Table<LocalUser>().ToListAsync();
    }

    public async Task<LocalUser?> GetUserAsync(string userId)
    {
        await InitAsync();
        return await _database!.FindAsync<LocalUser>(userId);
    }

    public async Task SaveUserAsync(LocalUser user)
    {
        await InitAsync();
        await _database!.InsertOrReplaceAsync(user);
    }

    public async Task SaveUsersAsync(IEnumerable<LocalUser> users)
    {
        await InitAsync();
        foreach (var user in users)
        {
            await _database!.InsertOrReplaceAsync(user);
        }
    }

    // Clear all data (for logout)
    public async Task ClearAllDataAsync()
    {
        await InitAsync();
        await _database!.DeleteAllAsync<LocalMessage>();
        await _database!.DeleteAllAsync<LocalConversation>();
        await _database!.DeleteAllAsync<LocalUser>();
    }
}
