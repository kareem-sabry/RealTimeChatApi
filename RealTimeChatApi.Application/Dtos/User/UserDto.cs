namespace RealTimeChatApi.Application.Dtos.User;

public record UserDto
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public bool IsOnline { get; init; }
    public DateTime? LastSeenAtUtc { get; init; }

}