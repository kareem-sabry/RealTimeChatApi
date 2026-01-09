using System.ComponentModel.DataAnnotations;
using RealTimeChatApi.Core.Enums;
using RealTimeChatApi.Core.Interfaces;

namespace RealTimeChatApi.Core.Entities;

public class Message : IAuditable,IEntity
{
    [Key]
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = String.Empty;

    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    public bool IsDeleted { get; set; }

    public DateTime SentAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }
}