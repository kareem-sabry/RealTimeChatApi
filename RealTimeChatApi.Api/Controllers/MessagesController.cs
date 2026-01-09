using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeChatApi.Application.Dtos.Message;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Models;

namespace RealTimeChatApi.Api.Controllers;

[ApiController]
[Route("api/conversations/{conversationId:int}/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] QueryParameters parameters)
    {
        if (conversationId <= 0)
        {
            return BadRequest(new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.InvalidId
            });
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

        try
        {
            var messages = await _messageService.GetConversationMessagesAsync(conversationId, userId, parameters);
            return Ok(messages);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new BasicResponse
            {
                Succeeded = false,
                Message = ex.Message
            });
        }
    }

    [HttpPost("mark-read")]
    [ProducesResponseType(typeof(BasicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkAsRead(int conversationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.InvalidUserContext
            });
        }

        var response = await _messageService.MarkMessagesAsReadAsync(conversationId, userId);

        if (response.Succeeded)
            return Ok(response);

        return BadRequest(response);
    }
}