using Microsoft.EntityFrameworkCore;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Infrastructure.Data;

namespace RealTimeChatApi.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
    }

    public async Task<List<User>> SearchUsersAsync(string searchTerm, Guid currentUserId)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _context.Users.Where(u => u.Id != currentUserId &&
                                               (
                                                   u.FirstName.ToLower().Contains(lowerSearchTerm) ||
                                                   u.LastName.ToLower().Contains(lowerSearchTerm) ||
                                                   u.Email!.ToLower().Contains(lowerSearchTerm)
                                               )).Take(20).ToListAsync();
    }
}