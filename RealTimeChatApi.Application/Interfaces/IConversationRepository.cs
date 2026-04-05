using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int id);
    Task<Conversation?> GetConversationBetweenUsersAsync(Guid user1Id, Guid user2Id);
    Task<List<Conversation>> GetUserConversationsAsync(Guid userId);
    Task<Conversation?> GetConversationWithMessagesAsync(int conversationId, Guid userId);
    Task AddAsync(Conversation conversation);
    void Delete(Conversation conversation);
    Task<bool> IsUserParticipantAsync(int conversationId, Guid userId);
    Task UpdateParticipantLastReadMessageAsync(int conversationId, Guid userId, int lastReadMessageId);
}
}