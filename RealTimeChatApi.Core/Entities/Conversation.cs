using System.ComponentModel.DataAnnotations;
using RealTimeChatApi.Core.Interfaces;

namespace RealTimeChatApi.Core.Entities;

public class Conversation : IAuditable,IEntity
{
    [Key]
    public int Id { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}