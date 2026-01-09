using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeChatApi.Application.Dtos.Conversation;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;

namespace RealTimeChatApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationsController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ConversationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserConversations()
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

        var conversations = await _conversationService.GetUserConversationsAsync(userId);
        return Ok(conversations);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationById(int id)
    {
        if (id <= 0)
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

        var conversation = await _conversationService.GetConversationByIdAsync(id, userId);
        if (conversation == null)
        {
            return NotFound(new BasicResponse
            {
                Succeeded = false,
                Message = ErrorMessages.ConversationNotFound
            });
        }

        return Ok(conversation);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

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
            var conversation = await _conversationService.StartConversationAsync(userId, request.OtherUserId);
            return CreatedAtAction(nameof(GetConversationById), new { id = conversation.Id }, conversation);
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

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(BasicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        if (id <= 0)
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

        var response = await _conversationService.DeleteConversationAsync(id, userId);

        if (response.Succeeded)
            return Ok(response);

        return NotFound(response);
    }
}