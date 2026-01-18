using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Infrastructure.Services;
using RealTimeChatApi.Tests.Helpers;

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

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@test.com",
            Password = "Password123!"
        };

        var user = TestHelper.CreateTestUser(request.Email);
        var jwtToken = "fake-jwt-token";
        var refreshToken = "fake-refresh-token";
        var expiresAt = _mockDateTimeProvider.Object.UtcNow.AddMinutes(60);

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        _mockTokenProcessor.Setup(x => x.GenerateJwtToken(user))
            .Returns((jwtToken, expiresAt));
        _mockTokenProcessor.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.LoginSuccessful);
        result.AccessToken.Should().Be(jwtToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.ExpiresAtUtc.Should().Be(expiresAt);

        user.RefreshToken.Should().Be(refreshToken);
        user.RefreshTokenExpiresAtUtc.Should().NotBeNull();
        user.IsOnline.Should().BeTrue();
        user.LastSeenAtUtc.Should().Be(_mockDateTimeProvider.Object.UtcNow);

        _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "Password123!"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.InvalidCredentials);
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();

        _mockTokenProcessor.Verify(x => x.GenerateJwtToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@test.com",
            Password = "WrongPassword"
        };

        var user = TestHelper.CreateTestUser(request.Email);

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.InvalidCredentials);

        _mockTokenProcessor.Verify(x => x.GenerateJwtToken(It.IsAny<User>()),
            Times.Never);
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var oldRefreshToken = "valid-refresh-token";
        var request = new RefreshTokenRequest
        {
            RefreshToken = oldRefreshToken
        };

        var user = TestHelper.CreateTestUser();
        user.RefreshToken = oldRefreshToken;
        user.RefreshTokenExpiresAtUtc = _mockDateTimeProvider.Object.UtcNow.AddDays(1);

        var newJwtToken = "new-jwt-token";
        var newRefreshToken = "new-refresh-token";
        var expiresAt = _mockDateTimeProvider.Object.UtcNow.AddMinutes(60);

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.GetUserByRefreshTokenAsync(oldRefreshToken))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        _mockTokenProcessor.Setup(x => x.GenerateJwtToken(user))
            .Returns((newJwtToken, expiresAt));
        _mockTokenProcessor.Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.TokenRefreshed);
        result.AccessToken.Should().Be(newJwtToken);
        result.RefreshToken.Should().Be(newRefreshToken);
        result.ExpiresAtUtc.Should().Be(expiresAt);

        user.RefreshToken.Should().Be(newRefreshToken);
        user.RefreshTokenExpiresAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsFailure()
    {
        // Arrange
        var expiredToken = "expired-refresh-token";
        var request = new RefreshTokenRequest
        {
            RefreshToken = expiredToken
        };

        var user = TestHelper.CreateTestUser();
        user.RefreshToken = expiredToken;
        user.RefreshTokenExpiresAtUtc = _mockDateTimeProvider.Object.UtcNow.AddDays(-1);

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.GetUserByRefreshTokenAsync(expiredToken))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.RefreshTokenExpired);

        _mockTokenProcessor.Verify(x => x.GenerateJwtToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-token"
        };

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.GetUserByRefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync((User?)null);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.RefreshTokenInvalid);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithMissingToken_ReturnsFailure()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = null
        };

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.RefreshTokenMissing);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidUser_ReturnsSuccess()
    {
        // Arrange
        var userEmail = "user@test.com";
        var user = TestHelper.CreateTestUser(userEmail);
        user.IsOnline = true;
        user.RefreshToken = "some-token";
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);

        _mockUserManager.Setup(x => x.FindByEmailAsync(userEmail))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.LogoutAsync(userEmail);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.LogoutSuccessful);

        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiresAtUtc.Should().BeNull();
        user.IsOnline.Should().BeFalse();
        user.LastSeenAtUtc.Should().Be(_mockDateTimeProvider.Object.UtcNow);

        _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userEmail = "nonexistent@test.com";

        _mockUserManager.Setup(x => x.FindByEmailAsync(userEmail))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LogoutAsync(userEmail);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.UserNotFound);

        _mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region SearchUsersAsync Tests

    [Fact]
    public async Task SearchUsersAsync_WithMatchingUsers_ReturnsUserList()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var searchTerm = "john";

        var users = new List<User>
        {
            TestHelper.CreateTestUser("john1@test.com", "John", "Doe"),
            TestHelper.CreateTestUser("john2@test.com", "Johnny", "Smith")
        };

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.SearchUsersAsync(searchTerm, currentUserId))
            .ReturnsAsync(users);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.SearchUsersAsync(searchTerm, currentUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(u =>
        {
            u.Email.Should().NotBeNullOrEmpty();
            u.FirstName.Should().NotBeNullOrEmpty();
            u.LastName.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task SearchUsersAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var searchTerm = "nonexistent";

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.SearchUsersAsync(searchTerm, currentUserId))
            .ReturnsAsync(new List<User>());

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.SearchUsersAsync(searchTerm, currentUserId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}