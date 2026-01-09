using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApi.Application.Dtos.Conversation;

public record StartConversationRequest
{
    [Required] 
    public Guid OtherUserId { get; init; }
}