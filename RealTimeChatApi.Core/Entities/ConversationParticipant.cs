using RealTimeChatApi.Core.Interfaces;

namespace RealTimeChatApi.Core.Entities;

public class ConversationParticipant : TimestampedEntity
{
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime JoinedAtUtc { get; set; }

    public int? LastReadMessageId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}