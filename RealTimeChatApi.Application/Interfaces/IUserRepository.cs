using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
    Task<List<User>> SearchUsersAsync(string searchTerm, Guid currentUserId);
}