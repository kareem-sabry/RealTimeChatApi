using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApi.Application.Dtos.User;

public record RegisterRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2)]
    public required string FirstName { get; init; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2)]
    public required string LastName { get; init; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}