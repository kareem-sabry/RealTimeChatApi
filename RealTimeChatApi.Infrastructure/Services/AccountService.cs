using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IAuthTokenProcessor _authTokenProcessor;
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AccountService(
        IAuthTokenProcessor authTokenProcessor,
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        ILogger<AccountService> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _authTokenProcessor = authTokenProcessor;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BasicResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var userExists = await _userManager.FindByEmailAsync(request.Email) != null;
        if (userExists)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.UserAlreadyExists
            };
        }

        var user = User.Create(request.Email, request.FirstName, request.LastName, _dateTimeProvider.UtcNow);
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            _logger.LogWarning("User registration failed for {Email}", request.Email);
            return new BasicResponse
            {
                Succeeded = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        _logger.LogInformation("User registered successfully: {Email}", user.Email);
        return new BasicResponse
        {
            Succeeded = true,
            Message = SuccessMessages.RegistrationSuccessful
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            return new LoginResponse
            {
                Succeeded = false,
                Message = ErrorMessages.InvalidCredentials
            };
        }

        var (jwtToken, expirationDateInUtc) = _authTokenProcessor.GenerateJwtToken(user);
        var refreshTokenValue = _authTokenProcessor.GenerateRefreshToken();
        var refreshTokenExpirationDateInUtc = _dateTimeProvider.UtcNow.AddDays(ApplicationConstants.RefreshTokenExpirationDays);

        user.RefreshToken = refreshTokenValue;
        user.RefreshTokenExpiresAtUtc = refreshTokenExpirationDateInUtc;
        user.IsOnline = true;
        user.LastSeenAtUtc = _dateTimeProvider.UtcNow;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return new LoginResponse
        {
            Succeeded = true,
            Message = SuccessMessages.LoginSuccessful,
            AccessToken = jwtToken,
            ExpiresAtUtc = expirationDateInUtc,
            RefreshToken = refreshTokenValue
        };
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return new RefreshTokenResponse
            {
                Succeeded = false,
                Message = ErrorMessages.RefreshTokenMissing
            };
        }

        var user = await _unitOfWork.Users.GetUserByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Invalid refresh token attempt");
            return new RefreshTokenResponse
            {
                Succeeded = false,
                Message = ErrorMessages.RefreshTokenInvalid
            };
        }

        if (user.RefreshTokenExpiresAtUtc < _dateTimeProvider.UtcNow)
        {
            _logger.LogInformation("Expired refresh token used for user: {Email}", user.Email);
            return new RefreshTokenResponse
            {
                Succeeded = false,
                Message = ErrorMessages.RefreshTokenExpired
            };
        }

        var (jwtToken, expirationDateInUtc) = _authTokenProcessor.GenerateJwtToken(user);
        var refreshTokenValue = _authTokenProcessor.GenerateRefreshToken();
        var refreshExpirationDateInUtc = _dateTimeProvider.UtcNow.AddDays(ApplicationConstants.RefreshTokenExpirationDays);

        user.RefreshToken = refreshTokenValue;
        user.RefreshTokenExpiresAtUtc = refreshExpirationDateInUtc;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

        return new RefreshTokenResponse
        {
            Succeeded = true,
            Message = SuccessMessages.TokenRefreshed,
            AccessToken = jwtToken,
            ExpiresAtUtc = expirationDateInUtc,
            RefreshToken = refreshTokenValue
        };
    }

    public async Task<BasicResponse> LogoutAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            _logger.LogWarning("Logout attempted for non-existent user: {Email}", userEmail);
            return new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.UserNotFound
            };
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAtUtc = null;
        user.IsOnline = false;
        user.LastSeenAtUtc = _dateTimeProvider.UtcNow;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User logged out: {Email}", userEmail);

        return new BasicResponse
        {
            Succeeded = true,
            Message = SuccessMessages.LogoutSuccessful
        };
    }

    public async Task<List<UserDto>> SearchUsersAsync(string searchTerm, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.SearchUsersAsync(searchTerm, currentUserId, cancellationToken);

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email!,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsOnline = u.IsOnline,
            LastSeenAtUtc = u.LastSeenAtUtc
        }).ToList();
    }
}