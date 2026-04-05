using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Core.Models;

namespace RealTimeChatApi.Application.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<Message>> GetConversationMessagesAsync(int conversationId, QueryParameters parameters, CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    void Update(Message message);
    void Delete(Message message);
    Task<int> GetUnreadCountAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<Message>> GetUnreadMessagesAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
}