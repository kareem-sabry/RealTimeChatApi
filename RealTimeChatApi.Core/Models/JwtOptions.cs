namespace RealTimeChatApi.Core.Models;

public class JwtOptions
{
    public const string JwtOptionsKey = "JwtOptions";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationTimeInMinutes { get; set; }
}