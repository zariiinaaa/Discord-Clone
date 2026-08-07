using Discord.Authorization;
using Discord.Core.DTOs.Admin.Requests;
using Discord.Core.DTOs.Admin.Responses;
using Discord.Core.DTOs.Common;
using Discord.Core.Interfaces;
using Discord.Hubs;
using Discord.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/admin/servers")]
[Authorize(Policy = AuthorizationPolicyNames.PlatformAdmin)]
public class AdminServersController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IHubContext<ChatHub, IChatClient> _chatHubContext;
    private readonly ILogger<AdminServersController> _logger;

    public AdminServersController(
        IAdminService adminService,
        IFileStorageService fileStorageService,
        IHubContext<ChatHub, IChatClient> chatHubContext,
        ILogger<AdminServersController> logger)
    {
        _adminService = adminService;
        _fileStorageService = fileStorageService;
        _chatHubContext = chatHubContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<AdminServerListItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetServers(
        [FromQuery] AdminServerListQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetServersAsync(query, cancellationToken));
    }

    [HttpGet("{serverId:int}")]
    [ProducesResponseType(typeof(AdminServerDetailsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServer(int serverId, CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetServerByIdAsync(serverId, cancellationToken));
    }

    [HttpDelete("{serverId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteServer(
        int serverId,
        CancellationToken cancellationToken)
    {
        var deletionResult = await _adminService.DeleteServerAsync(
            serverId,
            cancellationToken);

        CleanupFiles(
            deletionResult.ServerId,
            deletionResult.IconUrl,
            deletionResult.AttachmentUrls);

        await NotifyFormerMembersAsync(
            deletionResult.ServerId,
            deletionResult.MemberUserIds);

        return NoContent();
    }

    private void CleanupFiles(
        int serverId,
        string? iconUrl,
        IEnumerable<string> attachmentUrls)
    {
        try
        {
            _fileStorageService.DeleteServerIcon(iconUrl);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Admin server deletion committed, but icon cleanup failed. ServerId: {ServerId}, IconUrl: {IconUrl}",
                serverId,
                iconUrl);
        }

        foreach (var attachmentUrl in attachmentUrls)
        {
            try
            {
                _fileStorageService.DeleteMessageAttachment(attachmentUrl);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Admin server deletion committed, but attachment cleanup failed. ServerId: {ServerId}, FileUrl: {FileUrl}",
                    serverId,
                    attachmentUrl);
            }
        }
    }

    private async Task NotifyFormerMembersAsync(
        int serverId,
        IEnumerable<int> memberUserIds)
    {
        var notifications = memberUserIds
            .Distinct()
            .Select(userId => NotifyFormerMemberAsync(serverId, userId));

        await Task.WhenAll(notifications);
    }

    private async Task NotifyFormerMemberAsync(int serverId, int userId)
    {
        try
        {
            await _chatHubContext.Clients
                .Group(ChatHub.GetUserGroupName(userId))
                .ServerMemberRemoved(
                    serverId,
                    "Server platform administratoru tərəfindən silindi.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Admin server deletion committed, but realtime notification failed. ServerId: {ServerId}, UserId: {UserId}",
                serverId,
                userId);
        }
    }
}
