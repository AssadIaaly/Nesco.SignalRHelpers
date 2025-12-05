using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatApplication.Data;
using ChatApplication.Models;
using Nesco.SignalRUserManagement.Client.Services;

namespace ChatApplication.Services;

/// <summary>
/// Chat service for client-to-server communication.
/// Server-to-client events are handled by ChatMethodExecutor via the library's IMethodExecutor pattern.
/// Events are static to ensure they work across different service instances.
/// </summary>
public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly UserConnectionClient _connectionClient;
    private readonly AuthService _authService;
    private readonly ChatDatabase _database;

    // Static events raised by ChatMethodExecutor when server invokes methods on client
    public static event Action<NewMessageEvent>? OnMessageReceived;
    public static event Action<MessageDeliveredEvent>? OnMessageDelivered;
    public static event Action<MessagesReadEvent>? OnMessagesRead;
    public static event Action<UserStatusEvent>? OnUserStatusChanged;
    public static event Action<TypingNotification>? OnUserTyping;
    public static event Action<ForceLogoutEvent>? OnForceLogout;
    public static event Action? OnConversationsUpdated;

    // Static references for database operations in static methods
    private static ChatDatabase? _staticDatabase;
    private static UserConnectionClient? _staticConnectionClient;
    private static AuthService? _staticAuthService;

    public ChatService(
        HttpClient httpClient,
        UserConnectionClient connectionClient,
        AuthService authService,
        ChatDatabase database)
    {
        _httpClient = httpClient;
        _connectionClient = connectionClient;
        _authService = authService;
        _database = database;

        // Store static references for use in static Raise methods
        _staticDatabase = database;
        _staticConnectionClient = connectionClient;
        _staticAuthService = authService;
    }

    // Static methods called by ChatMethodExecutor to raise events
    internal static async Task RaiseMessageReceivedAsync(NewMessageEvent message)
    {
        if (_staticDatabase != null)
        {
            // Save to local database
            await _staticDatabase.SaveMessageAsync(new LocalMessage
            {
                Id = message.Id.ToString(),
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                ReceiverId = _staticAuthService?.UserId ?? string.Empty,
                Text = message.Text,
                SentAt = message.SentAt,
                Status = 1, // Delivered
                DeliveredAt = DateTime.UtcNow,
                IsMine = false
            });

            // Update conversation
            await _staticDatabase.UpdateConversationLastMessageAsync(
                message.ConversationId,
                message.Text.Length > 50 ? message.Text[..50] + "..." : message.Text,
                message.SentAt);
            await _staticDatabase.IncrementUnreadCountAsync(message.ConversationId);
        }

        // Mark as delivered on server
        try
        {
            if (_staticConnectionClient != null)
            {
                await _staticConnectionClient.SendAsync("MarkAsDelivered", message.Id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error marking as delivered: {ex.Message}");
        }

        OnMessageReceived?.Invoke(message);
        OnConversationsUpdated?.Invoke();
    }

    internal static async Task RaiseMessageDeliveredAsync(MessageDeliveredEvent evt)
    {
        if (_staticDatabase != null)
        {
            await _staticDatabase.UpdateMessageStatusAsync(evt.MessageId.ToString(), 1, evt.DeliveredAt);
        }
        OnMessageDelivered?.Invoke(evt);
    }

    internal static async Task RaiseMessagesReadAsync(MessagesReadEvent evt)
    {
        if (_staticDatabase != null && evt.MessageIds != null)
        {
            foreach (var id in evt.MessageIds)
            {
                await _staticDatabase.UpdateMessageStatusAsync(id.ToString(), 2, readAt: evt.ReadAt);
            }
        }
        OnMessagesRead?.Invoke(evt);
    }

    internal static void RaiseUserStatusChanged(UserStatusEvent evt)
    {
        OnUserStatusChanged?.Invoke(evt);
    }

    internal static void RaiseUserTyping(TypingNotification notification)
    {
        OnUserTyping?.Invoke(notification);
    }

    internal static void RaiseForceLogout(ForceLogoutEvent evt)
    {
        OnForceLogout?.Invoke(evt);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_authService.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.AccessToken);
        }
        else
        {
            Console.WriteLine("[ChatService] Warning: No access token available");
        }
        return request;
    }

    // API Methods
    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "api/users");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[ChatService] GetUsers failed: {response.StatusCode}");
                // Return cached users
                var cached = await _database.GetUsersAsync();
                return cached.Select(u => new UserDto(
                    u.Id, u.Email, u.DisplayName, u.LastSeen, false, u.ProfilePictureUrl
                )).ToList();
            }

            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();

            // Cache users locally
            if (users != null)
            {
                var localUsers = users.Select(u => new LocalUser
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    LastSeen = u.LastSeen,
                    ProfilePictureUrl = u.ProfilePictureUrl
                });
                await _database.SaveUsersAsync(localUsers);
            }

            return users ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error fetching users: {ex.Message}");
            // Return cached users
            var cached = await _database.GetUsersAsync();
            return cached.Select(u => new UserDto(
                u.Id, u.Email, u.DisplayName, u.LastSeen, false, u.ProfilePictureUrl
            )).ToList();
        }
    }

    public async Task<List<ConversationDto>> GetConversationsAsync()
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "api/chat/conversations");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[ChatService] GetConversations failed: {response.StatusCode}");
                // Return cached conversations
                var cached = await _database.GetConversationsAsync();
                return cached.Select(c => new ConversationDto(
                    c.Id,
                    new UserDto(c.OtherUserId, c.OtherUserEmail, c.OtherUserDisplayName, null, false, null),
                    c.CreatedAt,
                    c.LastMessageAt,
                    c.LastMessagePreview,
                    c.UnreadCount
                )).ToList();
            }

            var conversations = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();

            // Cache conversations locally
            if (conversations != null)
            {
                var localConversations = conversations.Select(c => new LocalConversation
                {
                    Id = c.Id,
                    OtherUserId = c.OtherUser.Id,
                    OtherUserEmail = c.OtherUser.Email,
                    OtherUserDisplayName = c.OtherUser.DisplayName,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.LastMessageAt,
                    LastMessagePreview = c.LastMessagePreview,
                    UnreadCount = c.UnreadCount
                });
                await _database.SaveConversationsAsync(localConversations);
            }

            return conversations ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error fetching conversations: {ex.Message}");
            // Return cached conversations
            var cached = await _database.GetConversationsAsync();
            return cached.Select(c => new ConversationDto(
                c.Id,
                new UserDto(c.OtherUserId, c.OtherUserEmail, c.OtherUserDisplayName, null, false, null),
                c.CreatedAt,
                c.LastMessageAt,
                c.LastMessagePreview,
                c.UnreadCount
            )).ToList();
        }
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(string conversationId, int skip = 0, int take = 50)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"api/chat/conversations/{conversationId}/messages?skip={skip}&take={take}");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[ChatService] GetMessages failed: {response.StatusCode}");
                // Return cached messages
                var cached = await _database.GetMessagesAsync(conversationId, skip, take);
                cached.Reverse();
                return cached.Select(m => new ChatMessageDto(
                    Guid.Parse(m.Id),
                    m.SenderId,
                    m.ReceiverId,
                    m.ConversationId,
                    m.Text,
                    m.SentAt,
                    m.DeliveredAt,
                    m.ReadAt,
                    (MessageStatus)m.Status,
                    m.IsMine
                )).ToList();
            }

            var messages = await response.Content.ReadFromJsonAsync<List<ChatMessageDto>>();

            // Cache messages locally
            if (messages != null)
            {
                var localMessages = messages.Select(m => new LocalMessage
                {
                    Id = m.Id.ToString(),
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Text = m.Text,
                    SentAt = m.SentAt,
                    DeliveredAt = m.DeliveredAt,
                    ReadAt = m.ReadAt,
                    Status = (int)m.Status,
                    IsMine = m.IsMine
                });
                await _database.SaveMessagesAsync(localMessages);
            }

            return messages ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error fetching messages: {ex.Message}");
            // Return cached messages
            var cached = await _database.GetMessagesAsync(conversationId, skip, take);
            cached.Reverse(); // Reverse to get chronological order
            return cached.Select(m => new ChatMessageDto(
                Guid.Parse(m.Id),
                m.SenderId,
                m.ReceiverId,
                m.ConversationId,
                m.Text,
                m.SentAt,
                m.DeliveredAt,
                m.ReadAt,
                (MessageStatus)m.Status,
                m.IsMine
            )).ToList();
        }
    }

    public async Task<ConversationDto?> StartConversationAsync(string otherUserId)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, $"api/chat/conversations/start/{otherUserId}");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ConversationDto>();
            }

            Console.WriteLine($"[ChatService] StartConversation failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error starting conversation: {ex.Message}");
        }
        return null;
    }

    public async Task<SendMessageResponse?> SendMessageAsync(string receiverId, string text)
    {
        if (!_connectionClient.IsConnected)
        {
            Console.WriteLine("[ChatService] Cannot send message: not connected");
            return null;
        }

        try
        {
            var response = await _connectionClient.InvokeAsync<SendMessageResponse>(
                "SendMessage",
                new SendMessageRequest(receiverId, text));

            if (response != null)
            {
                // Save to local database
                await _database.SaveMessageAsync(new LocalMessage
                {
                    Id = response.MessageId.ToString(),
                    ConversationId = response.ConversationId,
                    SenderId = _authService.UserId ?? string.Empty,
                    ReceiverId = receiverId,
                    Text = text,
                    SentAt = response.SentAt,
                    Status = 0, // Sent
                    IsMine = true
                });

                // Update conversation
                var preview = text.Length > 50 ? text[..50] + "..." : text;
                await _database.UpdateConversationLastMessageAsync(response.ConversationId, preview, response.SentAt);

                OnConversationsUpdated?.Invoke();
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error sending message: {ex.Message}");
            return null;
        }
    }

    public async Task MarkAsReadAsync(string conversationId)
    {
        if (!_connectionClient.IsConnected)
            return;

        try
        {
            await _connectionClient.SendAsync("MarkAsRead", new MarkAsReadRequest(conversationId));
            await _database.ClearUnreadCountAsync(conversationId);
            OnConversationsUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error marking as read: {ex.Message}");
        }
    }

    public async Task StartTypingAsync(string conversationId)
    {
        if (!_connectionClient.IsConnected)
            return;

        try
        {
            await _connectionClient.SendAsync("StartTyping", conversationId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error sending typing indicator: {ex.Message}");
        }
    }

    public async Task StopTypingAsync(string conversationId)
    {
        if (!_connectionClient.IsConnected)
            return;

        try
        {
            await _connectionClient.SendAsync("StopTyping", conversationId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error stopping typing indicator: {ex.Message}");
        }
    }

    public async Task ClearLocalDataAsync()
    {
        await _database.ClearAllDataAsync();
    }
}
