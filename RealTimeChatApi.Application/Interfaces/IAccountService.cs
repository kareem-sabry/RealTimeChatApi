using RealTimeChatApi.Application.Dtos.User;

namespace RealTimeChatApi.Application.Interfaces;

public interface IAccountService
{
    Task<BasicResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> LogoutAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<List<UserDto>> SearchUsersAsync(string searchTerm, Guid currentUserId, CancellationToken cancellationToken = default);
}