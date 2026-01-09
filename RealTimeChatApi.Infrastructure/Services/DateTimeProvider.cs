using RealTimeChatApi.Application.Interfaces;

namespace RealTimeChatApi.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}