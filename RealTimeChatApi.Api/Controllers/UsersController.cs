using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;

namespace RealTimeChatApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAccountService _accountService;

    public UsersController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Ok(new List<UserDto>());
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new BasicResponse
                {
                    Succeeded = false,
                    Message = ErrorMessages.InvalidUserContext
                });
            }

            var users = await _accountService.SearchUsersAsync(searchTerm, userId, cancellationToken);
            return Ok(users);
        }
    }
}