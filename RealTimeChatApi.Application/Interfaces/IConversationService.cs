using RealTimeChatApi.Application.Dtos.Conversation;
using RealTimeChatApi.Application.Dtos.User;

namespace RealTimeChatApi.Application.Interfaces;

public interface IConversationService
{
    Task<List<ConversationListDto>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ConversationDetailDto?> GetConversationByIdAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<ConversationDetailDto> StartConversationAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken = default);
    Task<BasicResponse> DeleteConversationAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
}