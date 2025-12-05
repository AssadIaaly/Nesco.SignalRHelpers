using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatApplication.Wasm.Models;
using Nesco.SignalRUserManagement.Client.Authorization.Services;
using Nesco.SignalRUserManagement.Client.Services;

namespace ChatApplication.Wasm.Services;

/// <summary>
/// Chat service for client-to-server communication.
/// Server-to-client events are handled by ChatMethodExecutor via the library's IMethodExecutor pattern.
/// Events are static to ensure they work across different service instances (scoped vs singleton).
/// </summary>
public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly UserConnectionClient _connectionClient;
    private readonly AuthService _authService;

    // Static events raised by ChatMethodExecutor when server invokes methods on client
    // Static because ChatMethodExecutor (singleton) may have different ChatService instance than UI components (scoped)
    public static event Action<NewMessageEvent>? OnMessageReceived;
    public static event Action<MessageDeliveredEvent>? OnMessageDelivered;
    public static event Action<MessagesReadEvent>? OnMessagesRead;
    public static event Action<UserStatusEvent>? OnUserStatusChanged;
    public static event Action<TypingNotification>? OnUserTyping;
    public static event Action<ForceLogoutEvent>? OnForceLogout;
    public static event Action? OnConversationsUpdated;

    public ChatService(
        HttpClient httpClient,
        UserConnectionClient connectionClient,
        AuthService authService)
    {
        _httpClient = httpClient;
        _connectionClient = connectionClient;
        _authService = authService;
    }

    // Methods called by ChatMethodExecutor to raise events
    internal static void RaiseMessageReceived(NewMessageEvent message)
    {
        OnMessageReceived?.Invoke(message);
        OnConversationsUpdated?.Invoke();
    }

    internal static void RaiseMessageDelivered(MessageDeliveredEvent evt)
    {
        OnMessageDelivered?.Invoke(evt);
    }

    internal static void RaiseMessagesRead(MessagesReadEvent evt)
    {
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
                return [];
            }

            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
            return users ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error fetching users: {ex.Message}");
            return [];
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
                return [];
            }

            var conversations = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
            return conversations ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error fetching conversations: {ex.Message}");
            return [];
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
                return [];
            }

            var messages = await response.Content.ReadFromJsonAsync<List<ChatMessageDto>>();
            return messages ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error fetching messages: {ex.Message}");
            return [];
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

    public async Task MarkAsDeliveredAsync(Guid messageId)
    {
        if (!_connectionClient.IsConnected)
            return;

        try
        {
            await _connectionClient.SendAsync("MarkAsDelivered", messageId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] Error marking as delivered: {ex.Message}");
        }
    }
}
