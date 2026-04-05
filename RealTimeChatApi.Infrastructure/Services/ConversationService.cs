using AutoMapper;
using Microsoft.Extensions.Logging;
using RealTimeChatApi.Application.Dtos.Conversation;
using RealTimeChatApi.Application.Dtos.Message;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConversationService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public ConversationService(IUnitOfWork unitOfWork, ILogger<ConversationService> logger, IDateTimeProvider dateTimeProvider, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<List<ConversationListDto>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var conversations = await _unitOfWork.Conversations.GetUserConversationsAsync(userId, cancellationToken);

        var conversationDtos = new List<ConversationListDto>();

        foreach (var conversation in conversations)
        {
            var otherUser = conversation.Participants.FirstOrDefault(p => p.UserId != userId)?.User;
            if (otherUser == null) continue;

            var lastMessage = conversation.Messages.OrderByDescending(m => m.SentAtUtc).FirstOrDefault();
            var unreadCount = await _unitOfWork.Messages.GetUnreadCountAsync(conversation.Id, userId, cancellationToken);

            conversationDtos.Add(new ConversationListDto
            {
                Id = conversation.Id,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.FullName,
                OtherUserIsOnline = otherUser.IsOnline,
                OtherUserLastSeenAtUtc = otherUser.LastSeenAtUtc,
                LastMessageContent = lastMessage?.Content,
                LastMessageTime = lastMessage?.SentAtUtc,
                UnreadCount = unreadCount,
                CreatedAtUtc = conversation.CreatedAtUtc
            });
        }

        return conversationDtos.OrderByDescending(c => c.LastMessageTime ?? c.CreatedAtUtc).ToList();
    }

    public async Task<ConversationDetailDto?> GetConversationByIdAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _unitOfWork.Conversations.GetConversationWithMessagesAsync(conversationId, userId, cancellationToken);
        if (conversation == null)
        {
            _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
            return null;
        }

        var otherUser = conversation.Participants.FirstOrDefault(p => p.UserId != userId)?.User;
        if (otherUser == null) return null;

        var messageDtos = _mapper.Map<List<MessageDto>>(
            conversation.Messages
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.SentAtUtc)
                .ToList());

        return new ConversationDetailDto
        {
            Id = conversation.Id,
            OtherUserId = otherUser.Id,
            OtherUserName = otherUser.FullName,
            OtherUserIsOnline = otherUser.IsOnline,
            OtherUserLastSeenAtUtc = otherUser.LastSeenAtUtc,
            Messages = messageDtos,
            CreatedAtUtc = conversation.CreatedAtUtc
        };
    }

    public async Task<ConversationDetailDto> StartConversationAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken = default)
    {
        if (currentUserId == otherUserId)
        {
            throw new InvalidOperationException(ErrorMessages.CannotMessageYourself);
        }

        // Check if conversation already exists
        var existingConversation = await _unitOfWork.Conversations.GetConversationBetweenUsersAsync(currentUserId, otherUserId, cancellationToken);
        if (existingConversation != null)
        {
            return (await GetConversationByIdAsync(existingConversation.Id, currentUserId, cancellationToken))!;
        }

        await _unitOfWork.BeginTransactionAsync();

        var conversation = new Conversation();
        await _unitOfWork.Conversations.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var participant1 = new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = currentUserId,
            JoinedAtUtc = _dateTimeProvider.UtcNow
        };

        var participant2 = new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = otherUserId,
            JoinedAtUtc = _dateTimeProvider.UtcNow
        };

        conversation.Participants.Add(participant1);
        conversation.Participants.Add(participant2);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync();

        _logger.LogInformation("Conversation created between {User1} and {User2}", currentUserId, otherUserId);

        return (await GetConversationByIdAsync(conversation.Id, currentUserId, cancellationToken))!;
    }

    public async Task<BasicResponse> DeleteConversationAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.ConversationNotFound
            };
        }

        var isParticipant = await _unitOfWork.Conversations.IsUserParticipantAsync(conversationId, userId, cancellationToken);
        if (!isParticipant)
        {
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.NotParticipantInConversation
            };
        }

        _unitOfWork.Conversations.Delete(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Conversation deleted: {ConversationId}", conversationId);

        return new BasicResponse
        {
            Succeeded = true,
            Message = SuccessMessages.ConversationDeleted
        };
    }
}