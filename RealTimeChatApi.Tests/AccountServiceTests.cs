using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Infrastructure.Services;

namespace RealTimeChatApi.Tests;

public class AccountServiceTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IAuthTokenProcessor> _mockTokenProcessor;
    private readonly Mock<ILogger<AccountService>> _mockLogger;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _mockUserManager = TestHelper.MockUserManager();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTokenProcessor = new Mock<IAuthTokenProcessor>();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();

        _mockDateTimeProvider.Setup(x => x.UtcNow)
            .Returns(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _sut = new AccountService(
            _mockTokenProcessor.Object,
            _mockUserManager.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockDateTimeProvider.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@test.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "Password123!"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.RegistrationSuccessful);

        _mockUserManager.Verify(x => x.CreateAsync(
            It.Is<User>(u =>
                u.Email == request.Email &&
                u.FirstName == request.FirstName &&
                u.LastName == request.LastName &&
                u.UserName == request.Email &&
                u.CreatedAtUtc == _mockDateTimeProvider.Object.UtcNow),
            request.Password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@test.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "Password123!"
        };

        var existingUser = TestHelper.CreateTestUser(request.Email);
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.UserAlreadyExists);

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), 
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenUserCreationFails_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@test.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "weak"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        var identityErrors = new[]
        {
            new IdentityError { Description = "Password is too weak" }
        };
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Password is too weak");
    }

    #endregion
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
}