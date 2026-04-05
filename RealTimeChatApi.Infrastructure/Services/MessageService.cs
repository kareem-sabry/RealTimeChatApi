using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealTimeChatApi.Application.Dtos.Message;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Core.Enums;
using RealTimeChatApi.Core.Models;
using RealTimeChatApi.Infrastructure.Data;

namespace RealTimeChatApi.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MessageService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MessageService(
        IUnitOfWork unitOfWork,
        ILogger<MessageService> logger,
        IDateTimeProvider dateTimeProvider
        )
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<MessageDto> SendMessageAsync(int conversationId, Guid senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(ErrorMessages.EmptyMessage);
        }

        if (content.Length > ApplicationConstants.MaxMessageLength)
        {
            throw new ArgumentException(ErrorMessages.MessageTooLong);
        }

        var isParticipant = await _unitOfWork.Conversations.IsUserParticipantAsync(conversationId, senderId);
        if (!isParticipant)
        {
            throw new InvalidOperationException(ErrorMessages.NotParticipantInConversation);
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content.Trim(),
            Status = MessageStatus.Sent,
            SentAtUtc = _dateTimeProvider.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Messages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        // Reload message with sender info
        var savedMessage = await _unitOfWork.Messages.GetByIdAsync(message.Id);

        _logger.LogInformation("Message sent in conversation {ConversationId} by {SenderId}", conversationId, senderId);

        return new MessageDto
        {
            Id = savedMessage!.Id,
            ConversationId = savedMessage.ConversationId,
            SenderId = savedMessage.SenderId,
            SenderName = savedMessage.Sender.FullName,
            Content = savedMessage.Content,
            Status = savedMessage.Status,
            IsDeleted = savedMessage.IsDeleted,
            SentAtUtc = savedMessage.SentAtUtc,
            EditedAtUtc = savedMessage.EditedAtUtc
        };
    }

    public async Task<PagedResult<MessageDto>> GetConversationMessagesAsync(
        int conversationId,
        Guid userId,
        QueryParameters parameters)
    {
        var isParticipant = await _unitOfWork.Conversations.IsUserParticipantAsync(conversationId, userId);
        if (!isParticipant)
        {
            throw new InvalidOperationException(ErrorMessages.NotParticipantInConversation);
        }

        var pagedMessages = await _unitOfWork.Messages.GetConversationMessagesAsync(conversationId, parameters);

        var messageDtos = pagedMessages.Items.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderName = m.Sender.FullName,
            Content = m.Content,
            Status = m.Status,
            IsDeleted = m.IsDeleted,
            SentAtUtc = m.SentAtUtc,
            EditedAtUtc = m.EditedAtUtc
        }).ToList();

        return new PagedResult<MessageDto>(
            messageDtos,
            pagedMessages.TotalCount,
            pagedMessages.CurrentPage,
            pagedMessages.PageSize);
    }

    public async Task<BasicResponse> DeleteMessageAsync(int messageId, Guid userId)
    {
        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null)
        {
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.MessageNotFound
            };
        }

        if (message.SenderId != userId)
        {
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.CannotDeleteOthersMessage
            };
        }

        message.IsDeleted = true;
        message.Content = "[Message deleted]";
        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Message deleted: {MessageId}", messageId);

        return new BasicResponse
        {
            Succeeded = true,
            Message = SuccessMessages.MessageDeleted
        };
    }

    public async Task<BasicResponse> MarkMessagesAsReadAsync(int conversationId, Guid userId)
    {
        var isParticipant = await _unitOfWork.Conversations.IsUserParticipantAsync(conversationId, userId);
        if (!isParticipant)
        {
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.NotParticipantInConversation
            };
        }

        var unreadMessages = await _unitOfWork.Messages.GetUnreadMessagesAsync(conversationId, userId);

        if (unreadMessages.Any())
        {
            foreach (var message in unreadMessages)
            {
                message.Status = MessageStatus.Read;
                // Entities returned by EF Core queries are already tracked.
                // Mutating a property is enough — calling Update() would force
                // a full-column UPDATE instead of a narrow single-column UPDATE.
            }

            var lastMessage = unreadMessages.OrderByDescending(m => m.Id).First();
            await _unitOfWork.Conversations.UpdateParticipantLastReadMessageAsync(
                conversationId, userId, lastMessage.Id);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} messages as read in conversation {ConversationId}",
                unreadMessages.Count, conversationId);
        }

        return new BasicResponse
        {
            Succeeded = true,
            Message = SuccessMessages.MessageMarkedAsRead
        };
    }

    public async Task<int> GetUnreadCountAsync(int conversationId, Guid userId)
    {
        return await _unitOfWork.Messages.GetUnreadCountAsync(conversationId, userId);
    }
}