using Discord.Core.DTOs.Conversations.Requests;
using Discord.Core.DTOs.Conversations.Responses;
using Discord.Core.DTOs.Messages.Requests;
using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IMessageService _messageService;

    public ConversationsController(IConversationService conversationService,IMessageService messageService)
    {
        _conversationService = conversationService;
        _messageService = messageService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<ConversationResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyConversations(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _conversationService
                .GetMyConversationsAsync(
                    userId,
                    cancellationToken);

        return Ok(result);
    }

    [HttpGet("{conversationId:int}")]
    [ProducesResponseType(
        typeof(ConversationResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int conversationId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _conversationService.GetByIdAsync(
                conversationId,
                userId,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost("direct")]
    [ProducesResponseType(
        typeof(ConversationResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDirectConversation(
        CreateDirectConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _conversationService
                .CreateDirectConversationAsync(
                    userId,
                    request,
                    cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPost("group")]
    [ProducesResponseType(
        typeof(ConversationResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGroupConversation(
        CreateGroupConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _conversationService
                .CreateGroupConversationAsync(
                    userId,
                    request,
                    cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPatch("{conversationId:int}/group")]
    [ProducesResponseType(
        typeof(ConversationResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroupConversation(
        int conversationId,
        UpdateGroupConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _conversationService
                .UpdateGroupConversationAsync(
                    conversationId,
                    userId,
                    request,
                    cancellationToken);

        return Ok(result);
    }

    [HttpPost("{conversationId:int}/members")]
    [ProducesResponseType(
        typeof(ConversationResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddGroupMember(
        int conversationId,
        AddGroupMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _conversationService
                .AddGroupMemberAsync(
                    conversationId,
                    userId,
                    request,
                    cancellationToken);

        return Ok(result);
    }

    [HttpDelete(
        "{conversationId:int}/members/{memberUserId:int}")]
    [ProducesResponseType(
        typeof(ConversationResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGroupMember(
        int conversationId,
        int memberUserId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _conversationService
                .RemoveGroupMemberAsync(
                    conversationId,
                    userId,
                    memberUserId,
                    cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{conversationId:int}/members/me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveGroupConversation(
        int conversationId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _conversationService
            .LeaveGroupConversationAsync(
                conversationId,
                userId,
                cancellationToken);

        return NoContent();
    }

    [HttpGet("{conversationId:int}/messages")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<MessageResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        int conversationId,
        [FromQuery] int? beforeMessageId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var result =
            await _messageService
                .GetConversationMessagesAsync(
                    conversationId,
                    userId,
                    beforeMessageId,
                    limit,
                    cancellationToken);

        return Ok(result);
    }

    [HttpPost("{conversationId:int}/messages")]
    [ProducesResponseType(
        typeof(MessageResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMessage(
        int conversationId,
        CreateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _messageService
                .CreateConversationMessageAsync(
                    conversationId,
                    userId,
                    request,
                    cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPatch(
    "{conversationId:int}/messages/{messageId:int}")]
    [ProducesResponseType(
    typeof(MessageResponseDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
    UpdateMessage(
        int conversationId,
        int messageId,
        UpdateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =
            await _messageService
                .UpdateConversationMessageAsync(
                    conversationId,
                    messageId,
                    userId,
                    request,
                    cancellationToken);

        return Ok(result);
    }

    [HttpDelete(
        "{conversationId:int}/messages/{messageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        DeleteMessage(
            int conversationId,
            int messageId,
            CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _messageService
            .DeleteConversationMessageAsync(
                conversationId,
                messageId,
                userId,
                cancellationToken);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException(
                "Access token etibarsızdır.");
        }

        return userId;
    }
}