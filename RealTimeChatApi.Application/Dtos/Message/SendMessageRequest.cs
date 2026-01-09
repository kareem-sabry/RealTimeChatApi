using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApi.Application.Dtos.Message;

public record SendMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [StringLength(5000, MinimumLength = 1)]
    public required string Content { get; init; }
}