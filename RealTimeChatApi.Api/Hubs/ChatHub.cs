using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealTimeChatApi.Application.Interfaces;

namespace RealTimeChatApi.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly ILogger<ChatHub> _logger;
    private static readonly ConcurrentDictionary<Guid, string> _userConnections = new();

    public ChatHub(IMessageService messageService, ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            _userConnections[userId] = Context.ConnectionId;
            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            _userConnections.TryRemove(userId, out _);
            _logger.LogInformation("User {UserId} disconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User joined conversation {ConversationId}", conversationId);
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User left conversation {ConversationId}", conversationId);
    }

    public async Task SendMessage(int conversationId, string content, CancellationToken cancellationToken = default)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid user context", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var message = await _messageService.SendMessageAsync(conversationId, userId, content, cancellationToken);

            // Send to all participants in the conversation
            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", message, cancellationToken: cancellationToken);

            _logger.LogInformation("Message sent in conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message, cancellationToken: cancellationToken);
        }
    }

    public async Task TypingStarted(int conversationId)
    {
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", userName, true);
    }

    public async Task TypingStopped(int conversationId)
    {
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", userName, false);
    }
}