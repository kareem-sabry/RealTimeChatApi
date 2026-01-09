namespace RealTimeChatApi.Application.Dtos.Conversation;

public class ConversationListDto
{
    public int Id { get; set; }
    public required string OtherUserName { get; set; }
    public Guid OtherUserId { get; set; }
    public bool OtherUserIsOnline { get; set; }
    public DateTime? OtherUserLastSeenAtUtc { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}