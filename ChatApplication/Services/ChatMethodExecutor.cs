using ChatApplication.Models;
using Nesco.SignalRUserManagement.Core.Interfaces;
using Nesco.SignalRUserManagement.Core.Utilities;

namespace ChatApplication.Services;

/// <summary>
/// Handles server-initiated method invocations on the client.
/// This is the proper library pattern - server calls methods via IUserCommunicatorService,
/// and this executor routes them to the appropriate handlers.
/// Calls static methods on ChatService to raise events.
/// </summary>
public class ChatMethodExecutor : IMethodExecutor
{
    public Task<object?> ExecuteAsync(string methodName, object? parameter)
    {
        Console.WriteLine($"[ChatMethodExecutor] Method invoked: {methodName}");

        return methodName switch
        {
            // Chat events from server
            "ReceiveMessage" => HandleReceiveMessage(parameter),
            "MessageDelivered" => HandleMessageDelivered(parameter),
            "MessagesRead" => HandleMessagesRead(parameter),
            "UserTyping" => HandleUserTyping(parameter),
            "UserStatusChanged" => HandleUserStatusChanged(parameter),
            "ForceLogout" => HandleForceLogout(parameter),

            // Standard utility methods
            "Ping" => Task.FromResult<object?>(new { message = "Pong", timestamp = DateTime.UtcNow }),
            "GetClientInfo" => Task.FromResult<object?>(new
            {
                platform = $"MAUI Blazor Hybrid ({DeviceInfo.Platform})",
                deviceModel = DeviceInfo.Model,
                deviceManufacturer = DeviceInfo.Manufacturer,
                osVersion = DeviceInfo.VersionString,
                idiom = DeviceInfo.Idiom.ToString(),
                appVersion = AppInfo.VersionString
            }),

            _ => Task.FromResult<object?>(null)
        };
    }

    private static async Task<object?> HandleReceiveMessage(object? parameter)
    {
        var message = ParameterParser.Parse<NewMessageEvent>(parameter);
        Console.WriteLine($"[ChatMethodExecutor] Received message from {message.SenderId}");
        await ChatService.RaiseMessageReceivedAsync(message);
        return new { success = true };
    }

    private static async Task<object?> HandleMessageDelivered(object? parameter)
    {
        var evt = ParameterParser.Parse<MessageDeliveredEvent>(parameter);
        Console.WriteLine($"[ChatMethodExecutor] Message {evt.MessageId} delivered");
        await ChatService.RaiseMessageDeliveredAsync(evt);
        return new { success = true };
    }

    private static async Task<object?> HandleMessagesRead(object? parameter)
    {
        var evt = ParameterParser.Parse<MessagesReadEvent>(parameter);
        Console.WriteLine($"[ChatMethodExecutor] Messages read in {evt.ConversationId}");
        await ChatService.RaiseMessagesReadAsync(evt);
        return new { success = true };
    }

    private static Task<object?> HandleUserTyping(object? parameter)
    {
        var notification = ParameterParser.Parse<TypingNotification>(parameter);
        ChatService.RaiseUserTyping(notification);
        return Task.FromResult<object?>(new { success = true });
    }

    private static Task<object?> HandleUserStatusChanged(object? parameter)
    {
        var evt = ParameterParser.Parse<UserStatusEvent>(parameter);
        Console.WriteLine($"[ChatMethodExecutor] User {evt.UserId} status: {(evt.IsOnline ? "online" : "offline")}");
        ChatService.RaiseUserStatusChanged(evt);
        return Task.FromResult<object?>(new { success = true });
    }

    private static Task<object?> HandleForceLogout(object? parameter)
    {
        var evt = ParameterParser.Parse<ForceLogoutEvent>(parameter);
        Console.WriteLine($"[ChatMethodExecutor] Force logout: {evt.Reason}");
        ChatService.RaiseForceLogout(evt);
        return Task.FromResult<object?>(new { success = true });
    }
}
