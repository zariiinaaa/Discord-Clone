using Discord.Core.DTOs.Users.Requests;
using Discord.Core.DTOs.Users.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IFileStorageService _fileStorageService;

    public UsersController(
        IUserService userService,
        IFileStorageService fileStorageService)
    {
        _userService = userService;
        _fileStorageService = fileStorageService;
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