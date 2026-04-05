using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationBetweenUsersAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationWithMessagesAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    void Delete(Conversation conversation);
    Task<bool> IsUserParticipantAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task UpdateParticipantLastReadMessageAsync(int conversationId, Guid userId, int lastReadMessageId, CancellationToken cancellationToken = default);
}