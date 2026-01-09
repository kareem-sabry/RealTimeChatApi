using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApi.Application.Dtos.User;

public record RefreshTokenRequest
{
    [Required]
    public string? RefreshToken { get; init; }
}