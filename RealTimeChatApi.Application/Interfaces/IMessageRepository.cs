using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Core.Models;

namespace RealTimeChatApi.Application.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(int id);
    Task<PagedResult<Message>> GetConversationMessagesAsync(int conversationId, QueryParameters parameters);
    Task AddAsync(Message message);
    void Update(Message message);
    void Delete(Message message);
    Task<int> GetUnreadCountAsync(int conversationId, Guid userId);
    Task<List<Message>> GetUnreadMessagesAsync(int conversationId, Guid userId);
}