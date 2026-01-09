using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Core.Models;

namespace RealTimeChatApi.Infrastructure.Services;

public class AuthTokenProcessorService : IAuthTokenProcessor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;

    public AuthTokenProcessorService(IOptions<JwtOptions> jwtOptions, IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
    }

    public (string jwtToken, DateTime expiresAtUtc) GenerateJwtToken(User user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName)
        };

        var expires = _dateTimeProvider.UtcNow.AddMinutes(_jwtOptions.ExpirationTimeInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwtToken, expires);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}