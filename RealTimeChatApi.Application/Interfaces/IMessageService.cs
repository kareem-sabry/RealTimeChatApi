using RealTimeChatApi.Application.Dtos.Message;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Core.Models;

namespace RealTimeChatApi.Application.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int conversationId, Guid senderId, string content);
    Task<PagedResult<MessageDto>> GetConversationMessagesAsync(int conversationId, Guid userId, QueryParameters parameters);
    Task<BasicResponse> DeleteMessageAsync(int messageId, Guid userId);
    Task<BasicResponse> MarkMessagesAsReadAsync(int conversationId, Guid userId);
    Task<int> GetUnreadCountAsync(int conversationId, Guid userId);
}