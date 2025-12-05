using ChatApplication.Server.Data;
using ChatApplication.Server.Models;
using ChatApplication.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Server.Hubs;
using Nesco.SignalRUserManagement.Server.Services;

namespace ChatApplication.Server.Hubs;

public class ChatHub : UserManagementHub
{
    private readonly InMemoryConnectionTracker _tracker;
    private readonly ILogger<ChatHub> _chatLogger;
    private readonly IChatService _chatService;
    private readonly IConversationService _conversationService;
    private readonly IUserConnectionService _userConnectionService;
    private readonly IUserCommunicatorService _communicatorService;
    private readonly ApplicationDbContext _appDbContext;

    public ChatHub(
        InMemoryConnectionTracker tracker,
        ILogger<UserManagementHub> logger,
        ILogger<ChatHub> chatLogger,
        IChatService chatService,
        IConversationService conversationService,
        IUserConnectionService userConnectionService,
        IUserCommunicatorService communicatorService,
        ApplicationDbContext appDbContext,
        IResponseManager? responseManager = null)
        : base(tracker, logger, responseManager)
    {
        _tracker = tracker;
        _chatLogger = chatLogger;
        _chatService = chatService;
        _conversationService = conversationService;
        _userConnectionService = userConnectionService;
        _communicatorService = communicatorService;
        _appDbContext = appDbContext;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            // Check for existing connections (single-session enforcement) using in-memory tracker
            var existingConnections = _tracker.GetUserConnections(userId)
                .Where(connId => connId != Context.ConnectionId)
                .ToList();

            if (existingConnections.Any())
            {
                // Notify existing connections to logout using IUserCommunicatorService
                foreach (var connectionId in existingConnections)
                {
                    _ = _communicatorService.InvokeMethodOnConnectionAsync(connectionId, "ForceLogout",
                        new ForceLogoutEvent("You have logged in from another device"));
                }

                // Remove old connections from in-memory tracker
                foreach (var connectionId in existingConnections)
                {
                    _tracker.RemoveConnection(connectionId);
                }

                _chatLogger.LogInformation("User {UserId} had existing connections removed due to new login", userId);
            }
        }

        await base.OnConnectedAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            // Update user's last seen
            var user = await _appDbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastSeen = DateTime.UtcNow;
                await _appDbContext.SaveChangesAsync();

                // Notify all users that this user is now online using IUserCommunicatorService
                var displayName = user.DisplayName ?? user.Email;
                _ = _communicatorService.InvokeMethodAsync("UserStatusChanged",
                    new UserStatusEvent(userId, true, null, displayName));
            }
            else
            {
                // User not found in database, still notify but without display name
                _ = _communicatorService.InvokeMethodAsync("UserStatusChanged",
                    new UserStatusEvent(userId, true, null));
            }

            _chatLogger.LogInformation("User {UserId} connected with ConnectionId {ConnectionId}", userId, Context.ConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            // Check if user has other active connections using in-memory tracker
            var otherConnections = _tracker.GetUserConnections(userId)
                .Any(connId => connId != Context.ConnectionId);

            if (!otherConnections)
            {
                // Update user's last seen only if no other connections
                var user = await _appDbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastSeen = DateTime.UtcNow;
                    await _appDbContext.SaveChangesAsync();

                    // Notify all users that this user is now offline using IUserCommunicatorService
                    _ = _communicatorService.InvokeMethodAsync("UserStatusChanged",
                        new UserStatusEvent(userId, false, user.LastSeen));
                }
            }

            _chatLogger.LogInformation("User {UserId} disconnected from ConnectionId {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<SendMessageResponse> SendMessage(SendMessageRequest request)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId))
        {
            throw new HubException("User not authenticated");
        }

        _chatLogger.LogInformation("User {SenderId} sending message to {ReceiverId}", senderId, request.ReceiverId);

        // Save message to database
        var message = await _chatService.SendMessageAsync(senderId, request.ReceiverId, request.Text);

        // Get sender info for the notification
        var sender = await _appDbContext.Users.FindAsync(senderId);
        var senderName = sender?.DisplayName ?? sender?.Email ?? "Unknown";

        // Notify receiver if online using IUserCommunicatorService (proper library pattern)
        var isReceiverOnline = _userConnectionService.IsUserConnected(request.ReceiverId);
        if (isReceiverOnline)
        {
            var newMessageEvent = new NewMessageEvent(
                message.Id,
                senderId,
                senderName,
                message.ConversationId,
                message.Text,
                message.SentAt
            );

            // Fire and forget - we don't need to wait for acknowledgment
            _ = _communicatorService.InvokeMethodOnUserAsync(request.ReceiverId, "ReceiveMessage", newMessageEvent);
        }

        return new SendMessageResponse(message.Id, message.ConversationId, message.SentAt);
    }

    public async Task MarkAsDelivered(Guid messageId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        var message = await _chatService.GetMessageAsync(messageId);
        if (message == null)
        {
            return;
        }

        // Only mark as delivered if we are the receiver
        if (message.ReceiverId != userId)
        {
            return;
        }

        await _chatService.MarkMessageAsDeliveredAsync(messageId);

        // Notify sender that message was delivered using IUserCommunicatorService
        var isSenderOnline = _userConnectionService.IsUserConnected(message.SenderId);
        if (isSenderOnline)
        {
            var deliveredEvent = new MessageDeliveredEvent(
                messageId,
                message.ConversationId,
                DateTime.UtcNow
            );

            _ = _communicatorService.InvokeMethodOnUserAsync(message.SenderId, "MessageDelivered", deliveredEvent);
        }
    }

    public async Task MarkAsRead(MarkAsReadRequest request)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        var messageIds = await _chatService.MarkConversationAsReadAsync(request.ConversationId, userId);

        if (!messageIds.Any())
        {
            return;
        }

        // Get the conversation to find the other user
        var conversation = await _conversationService.GetConversationAsync(request.ConversationId);
        if (conversation == null)
        {
            return;
        }

        var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

        // Notify the other user that their messages were read using IUserCommunicatorService
        var isOtherUserOnline = _userConnectionService.IsUserConnected(otherUserId);
        if (isOtherUserOnline)
        {
            var readEvent = new MessagesReadEvent(
                request.ConversationId,
                userId,
                DateTime.UtcNow,
                messageIds
            );

            _ = _communicatorService.InvokeMethodOnUserAsync(otherUserId, "MessagesRead", readEvent);
        }

        _chatLogger.LogInformation("User {UserId} marked {Count} messages as read in conversation {ConversationId}",
            userId, messageIds.Count, request.ConversationId);
    }

    public async Task StartTyping(string conversationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var conversation = await _conversationService.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            return;
        }

        var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

        if (_userConnectionService.IsUserConnected(otherUserId))
        {
            _ = _communicatorService.InvokeMethodOnUserAsync(otherUserId, "UserTyping",
                new TypingNotification(userId, conversationId, true));
        }
    }

    public async Task StopTyping(string conversationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var conversation = await _conversationService.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            return;
        }

        var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

        if (_userConnectionService.IsUserConnected(otherUserId))
        {
            _ = _communicatorService.InvokeMethodOnUserAsync(otherUserId, "UserTyping",
                new TypingNotification(userId, conversationId, false));
        }
    }
}
