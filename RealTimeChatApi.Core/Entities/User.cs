using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RealTimeChatApi.Core.Entities;

public class User : IdentityUser<Guid>
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }
    
    [MaxLength(500)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    public bool IsOnline { get; set; }
    
    public DateTime? LastSeenAtUtc { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ConversationParticipant> ConversationParticipants { get; set; } =
        new List<ConversationParticipant>();

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public static User Create(string email, string firstName, string lastName, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(email));
        }
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));
        }

        return new User
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
            IsOnline = false
        };
    }

    public string FullName => $"{FirstName} {LastName}";
}