using RealTimeChatApi.Application.Dtos.User;

namespace RealTimeChatApi.Application.Interfaces;

public interface IAccountService
{
    Task<BasicResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<BasicResponse> LogoutAsync(string userEmail);
    Task<List<UserDto>> SearchUsersAsync(string searchTerm, Guid currentUserId);
}