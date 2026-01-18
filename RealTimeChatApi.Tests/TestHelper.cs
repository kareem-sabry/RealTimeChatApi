using Microsoft.AspNetCore.Identity;
using Moq;
using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Tests;

public static  class TestHelper
{
    public static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    public static User CreateTestUser(
        string email = "test@example.com",
        string firstName = "John",
        string lastName = "Doe")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsOnline = false
        };
    }
}