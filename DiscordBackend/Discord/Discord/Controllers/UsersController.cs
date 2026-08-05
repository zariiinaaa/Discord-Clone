using Discord.Core.DTOs.Users.Requests;
using Discord.Core.DTOs.Users.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Hubs;
using Discord.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUserPresenceService _userPresenceService;
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    public UsersController(
        IUserService userService,
        IFileStorageService fileStorageService, IUserPresenceService userPresenceService,
        IHubContext<ChatHub, IChatClient> hubContext)
    {
        _userService = userService;
        _fileStorageService = fileStorageService;
        _userPresenceService = userPresenceService;
        _hubContext = hubContext;
    }

    [HttpGet("me")]
    [ProducesResponseType(
        typeof(UserResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _userService.GetByIdAsync(
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("me")]
    [ProducesResponseType(
        typeof(UserResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _userService.UpdateProfileAsync(
            userId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType(
        typeof(UserResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var currentUser =
            await _userService.GetByIdAsync(
                userId,
                cancellationToken);

        var newAvatarUrl =
            await _fileStorageService.SaveAvatarAsync(
                file,
                cancellationToken);

        UserResponseDto result;

        try
        {
            result = await _userService.UpdateAvatarAsync(
                userId,
                newAvatarUrl,
                cancellationToken);
        }
        catch
        {
            _fileStorageService.DeleteAvatar(newAvatarUrl);
            throw;
        }

        _fileStorageService.DeleteAvatar(
            currentUser.AvatarUrl);

        return Ok(result);
    }

    [HttpDelete("me/avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAvatar(
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var currentUser = await _userService.GetByIdAsync(
            userId,
            cancellationToken);

        await _userService.UpdateAvatarAsync(
            userId,
            null,
            cancellationToken);

        _fileStorageService.DeleteAvatar(
            currentUser.AvatarUrl);

        return NoContent();
    }

    [HttpPatch("me/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeStatus(ChangeStatusRequestDto request,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var visibleStatus =await _userPresenceService.ChangeStatusAsync(
        userId,request.Status,cancellationToken);

        await _hubContext.Clients.All.UserPresenceChanged(
            userId, visibleStatus);

        return Ok(new
        {
            preferredStatus = request.Status.ToString(),
            visibleStatus = visibleStatus.ToString()
        });
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