using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<List<User>> SearchUsersAsync(string searchTerm, Guid currentUserId, CancellationToken cancellationToken = default);
}