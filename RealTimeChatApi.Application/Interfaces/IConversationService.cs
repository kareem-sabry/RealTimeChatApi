using RealTimeChatApi.Application.Dtos.Conversation;
using RealTimeChatApi.Application.Dtos.User;

namespace RealTimeChatApi.Application.Interfaces;

public interface IConversationService
{
    Task<List<ConversationListDto>> GetUserConversationsAsync(Guid userId);
    Task<ConversationDetailDto?> GetConversationByIdAsync(int conversationId, Guid userId);
    Task<ConversationDetailDto> StartConversationAsync(Guid currentUserId, Guid otherUserId);
    Task<BasicResponse> DeleteConversationAsync(int conversationId, Guid userId);
}