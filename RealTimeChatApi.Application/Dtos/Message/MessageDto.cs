using RealTimeChatApi.Core.Enums;

namespace RealTimeChatApi.Application.Dtos.Message;

public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public required string SenderName { get; set; }
    public required string Content { get; set; }
    public MessageStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public bool IsDeleted { get; set; }
    public DateTime SentAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; } 
}