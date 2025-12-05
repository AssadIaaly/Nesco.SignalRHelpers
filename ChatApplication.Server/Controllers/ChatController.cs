using System.Security.Claims;
using ChatApplication.Server.Models;
using ChatApplication.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IConversationService _conversationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IConversationService conversationService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _conversationService = conversationService;
        _logger = logger;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationDto>>> GetConversations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var conversations = await _conversationService.GetUserConversationsAsync(userId);
        return Ok(conversations);
    }

    [HttpGet("conversations/{conversationId}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(string conversationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var conversation = await _conversationService.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            return NotFound();
        }

        // Ensure user is part of the conversation
        if (conversation.User1Id != userId && conversation.User2Id != userId)
        {
            return Forbid();
        }

        var conversations = await _conversationService.GetUserConversationsAsync(userId);
        var dto = conversations.FirstOrDefault(c => c.Id == conversationId);

        return dto != null ? Ok(dto) : NotFound();
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetMessages(
        string conversationId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var conversation = await _conversationService.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            return NotFound();
        }

        // Ensure user is part of the conversation
        if (conversation.User1Id != userId && conversation.User2Id != userId)
        {
            return Forbid();
        }

        var messages = await _chatService.GetMessagesAsync(conversationId, userId, skip, take);
        return Ok(messages);
    }

    [HttpPost("conversations/start/{otherUserId}")]
    public async Task<ActionResult<ConversationDto>> StartConversation(string otherUserId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (userId == otherUserId)
        {
            return BadRequest("Cannot start conversation with yourself");
        }

        var conversation = await _conversationService.GetOrCreateConversationAsync(userId, otherUserId);

        var conversations = await _conversationService.GetUserConversationsAsync(userId);
        var dto = conversations.FirstOrDefault(c => c.Id == conversation.Id);

        return dto != null ? Ok(dto) : NotFound();
    }

    [HttpGet("conversations/{conversationId}/unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount(string conversationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _chatService.GetUnreadCountAsync(conversationId, userId);
        return Ok(new { conversationId, unreadCount = count });
    }
}
