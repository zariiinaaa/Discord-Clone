using Discord.Core.DTOs.Messages.Requests;
using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/channels/{channelId:int}/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IHubContext<ChatHub, IChatClient> _chatHubContext;
    public MessagesController(IMessageService messageService,IHubContext<ChatHub, IChatClient> chatHubContext)
    {
        _messageService = messageService;
        _chatHubContext = chatHubContext;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<MessageResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        int channelId,
        [FromQuery] int? beforeMessageId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _messageService.GetChannelMessagesAsync(channelId,GetCurrentUserId(), beforeMessageId,
                limit,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(MessageResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int channelId,CreateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _messageService.CreateAsync(
            channelId,
            GetCurrentUserId(),
            request,
            cancellationToken);

        await _chatHubContext.Clients.Group(ChatHub.GetChannelGroupName(channelId)).MessageCreated(result);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPatch("{messageId:int}")]
    [ProducesResponseType(
        typeof(MessageResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update( int channelId,int messageId,
        UpdateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _messageService.UpdateAsync(
            channelId,
            messageId,
            GetCurrentUserId(),
            request,
            cancellationToken);

        await _chatHubContext.Clients.Group(ChatHub.GetChannelGroupName(channelId)).MessageUpdated(result);

        return Ok(result);
    }

    [HttpDelete("{messageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int channelId,int messageId,
        CancellationToken cancellationToken)
    {
        await _messageService.DeleteAsync(
            channelId,
            messageId,
            GetCurrentUserId(),
            cancellationToken);
        await _chatHubContext.Clients.Group(ChatHub.GetChannelGroupName(channelId)).MessageDeleted(channelId, messageId);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException("Access token etibarsızdır.");
        }

        return userId;
    }
}