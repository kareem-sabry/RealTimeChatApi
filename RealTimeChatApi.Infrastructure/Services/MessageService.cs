using AutoMapper;
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
    private readonly IMapper _mapper;

    public MessageService(
        IUnitOfWork unitOfWork,
        ILogger<MessageService> logger,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<MessageDto> SendMessageAsync(int conversationId, Guid senderId, string content,
        CancellationToken cancellationToken = default)

    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(ErrorMessages.EmptyMessage);
        }

        if (content.Length > ApplicationConstants.MaxMessageLength)
        {
            throw new ArgumentException(ErrorMessages.MessageTooLong);
        }

        var isParticipant =
            await _unitOfWork.Conversations.IsUserParticipantAsync(conversationId, senderId, cancellationToken);
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

        await _unitOfWork.Messages.AddAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload message with sender info
        var savedMessage = await _unitOfWork.Messages.GetByIdAsync(message.Id, cancellationToken);

        _logger.LogInformation("Message sent in conversation {ConversationId} by {SenderId}", conversationId, senderId);

        return _mapper.Map<MessageDto>(savedMessage!);
    }

    public async Task<PagedResult<MessageDto>> GetConversationMessagesAsync(
        int conversationId,
        Guid userId,
        QueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var isParticipant =
            await _unitOfWork.Conversations.IsUserParticipantAsync(conversationId, userId, cancellationToken);
        if (!isParticipant)
        {
            throw new InvalidOperationException(ErrorMessages.NotParticipantInConversation);
        }

        var pagedMessages =
            await _unitOfWork.Messages.GetConversationMessagesAsync(conversationId, parameters, cancellationToken);

        var messageDtos = _mapper.Map<List<MessageDto>>(pagedMessages.Items);
        
        return new PagedResult<MessageDto>(
            messageDtos,
            pagedMessages.TotalCount,
            pagedMessages.CurrentPage,
            pagedMessages.PageSize);
    }

    public async Task<BasicResponse> DeleteMessageAsync(int messageId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var message = await _unitOfWork.Messages.GetByIdAsync(messageId, cancellationToken);
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Message deleted: {MessageId}", messageId);

        return new BasicResponse
        {
            Succeeded = true,
            Message = SuccessMessages.MessageDeleted
        };
    }

    public async Task<BasicResponse> MarkMessagesAsReadAsync(int conversationId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var isParticipant =
            await _unitOfWork.Conversations.IsUserParticipantAsync(conversationId, userId, cancellationToken);
        if (!isParticipant)
        {
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.NotParticipantInConversation
            };
        }

        var unreadMessages =
            await _unitOfWork.Messages.GetUnreadMessagesAsync(conversationId, userId, cancellationToken);

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
                conversationId, userId, lastMessage.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked {Count} messages as read in conversation {ConversationId}",
                unreadMessages.Count, conversationId);
        }

        return new BasicResponse
        {
            Succeeded = true,
            Message = SuccessMessages.MessageMarkedAsRead
        };
    }

    public async Task<int> GetUnreadCountAsync(int conversationId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Messages.GetUnreadCountAsync(conversationId, userId, cancellationToken);
    }
}