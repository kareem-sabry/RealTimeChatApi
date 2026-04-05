using RealTimeChatApi.Application.Dtos.Message;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Core.Models;

namespace RealTimeChatApi.Application.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int conversationId, Guid senderId, string content, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageDto>> GetConversationMessagesAsync(int conversationId, Guid userId, QueryParameters parameters, CancellationToken cancellationToken = default);
    Task<BasicResponse> DeleteMessageAsync(int messageId, Guid userId, CancellationToken cancellationToken = default);
    Task<BasicResponse> MarkMessagesAsReadAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int conversationId, Guid userId, CancellationToken cancellationToken = default);
}