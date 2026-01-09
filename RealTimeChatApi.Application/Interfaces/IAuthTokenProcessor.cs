using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Application.Interfaces;

public interface IAuthTokenProcessor
{
    (string jwtToken, DateTime expiresAtUtc) GenerateJwtToken(User user);
    string GenerateRefreshToken();
}