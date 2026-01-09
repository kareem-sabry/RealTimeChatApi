using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApi.Application.Dtos.User;

public record LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required")]
    public required string Password { get; init; }
}