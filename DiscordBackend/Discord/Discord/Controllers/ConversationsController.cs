using Discord.Core.DTOs.Conversations.Requests;
using Discord.Core.DTOs.Conversations.Responses;
using Discord.Core.DTOs.Messages.Requests;
using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Core.Models.Files;
using Discord.Hubs;
using Discord.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IMessageService _messageService;

    private readonly IHubContext<ChatHub, IChatClient> _chatHubContext;

    private readonly IFileStorageService _fileStorageService;

    public ConversationsController(
        IConversationService conversationService,
        IMessageService messageService,
        IHubContext<ChatHub, IChatClient> chatHubContext,
        IFileStorageService fileStorageService)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _chatHubContext = chatHubContext;
        _fileStorageService = fileStorageService;
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
            await _conversationService.GetMyConversationsAsync(
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

        await _chatHubContext.Clients
            .User(userId.ToString())
            .ConversationDataChanged();

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

        await NotifyConversationMembersAsync(result);

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
    public async Task<IActionResult> RemoveGroupMember(int conversationId,int memberUserId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =await _conversationService.RemoveGroupMemberAsync(
        conversationId, userId, memberUserId, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{conversationId:int}/members/me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveGroupConversation(int conversationId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _conversationService.LeaveGroupConversationAsync(conversationId,userId,cancellationToken);

        return NoContent();
    }

    [HttpPut("{conversationId:int}/mute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMuteStatus(int conversationId,UpdateConversationMuteRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _conversationService.UpdateMuteStatusAsync(conversationId,userId,request,cancellationToken);

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
    public async Task<IActionResult> GetMessages(int conversationId,[FromQuery] int? beforeMessageId,
    [FromQuery] int limit = 50,CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var result =await _messageService.GetConversationMessagesAsync(
          conversationId,
           userId, beforeMessageId, limit, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{conversationId:int}/messages")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
        typeof(MessageResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMessage(int conversationId,[FromForm] CreateMessageRequestDto request,
        [FromForm] List<IFormFile>? attachments,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var conversation =await _conversationService.GetByIdAsync(
       conversationId,userId,cancellationToken);

        var storedAttachments = new List<StoredFileResult>();

        MessageResponseDto result;

        try
        {
            foreach (var file in
                     attachments ?? new List<IFormFile>())
            {
                var storedFile =await _fileStorageService.SaveMessageAttachmentAsync(file,
               cancellationToken);

                storedAttachments.Add(storedFile);
            }

            result =await _messageService.CreateConversationMessageAsync( conversationId,userId,request,storedAttachments,
               cancellationToken);
        }
        catch
        {
            foreach (var attachment in storedAttachments)
            {
                _fileStorageService.DeleteMessageAttachment(attachment.FileUrl);
            }

            throw;
        }

        var groupName = ChatHub.GetConversationGroupName(conversationId);

        await _chatHubContext.Clients.Group(groupName) .ConversationMessageCreated( conversationId,
         result);

        await NotifyConversationMembersAsync(conversation);

        if (conversation.Type == ConversationType.Direct)
        {
            var recipient =conversation.Members.FirstOrDefault( member => member.UserId != userId);

            if (recipient is not null)
            {
                await _chatHubContext.Clients.User(recipient.UserId.ToString())
                    .DirectMessageRequestCreated();
            }
        }

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
    public async Task<IActionResult> UpdateMessage(int conversationId,int messageId, UpdateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _messageService.UpdateConversationMessageAsync( conversationId, messageId, userId,
         request, cancellationToken);


        var groupName = ChatHub.GetConversationGroupName(conversationId);

        await _chatHubContext.Clients .Group(groupName).ConversationMessageUpdated( conversationId,
        result);

        return Ok(result);
    }

    [HttpDelete(
        "{conversationId:int}/messages/{messageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(int conversationId,int messageId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _messageService.DeleteConversationMessageAsync( conversationId,messageId,userId,
        cancellationToken);

        var groupName =ChatHub.GetConversationGroupName(conversationId);

        await _chatHubContext.Clients
            .Group(groupName)
            .ConversationMessageDeleted(
                conversationId,
                messageId);

        return NoContent();
    }

    [HttpPut("{conversationId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int conversationId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _conversationService.MarkAsReadAsync( conversationId,userId,cancellationToken);

        return NoContent();
    }

    private Task NotifyConversationMembersAsync(
        ConversationResponseDto conversation)
    {
        var memberUserIds = conversation.Members.Select(member =>member.UserId.ToString()).ToArray();

        if (memberUserIds.Length == 0)
        {
            return Task.CompletedTask;
        }

        return _chatHubContext.Clients.Users(memberUserIds)
            .ConversationDataChanged();
    }

    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException(
                "Access token etibarsızdır.");
        }

        return userId;
    }
}