using RealTimeChatApi.Application.Dtos.Message;

namespace RealTimeChatApi.Application.Dtos.Conversation;

public class ConversationDetailDto
{
    public int Id { get; set; }
    public Guid OtherUserId { get; set; }
    public required string OtherUserName { get; set; }
    public bool OtherUserIsOnline { get; set; }
    public DateTime? OtherUserLastSeenAtUtc { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
}