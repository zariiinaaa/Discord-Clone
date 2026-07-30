using Discord.Core.DTOs.Friends.Requests;
using Discord.Core.DTOs.Friends.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers
{
    [ApiController]
    [Route("api/v1/friends")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendsController( IFriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(IReadOnlyCollection<FriendResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFriends( CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result =await _friendService.GetFriendsAsync(userId, cancellationToken);

            return Ok(result);
        }

        [HttpGet("requests/incoming")]
        [ProducesResponseType(
            typeof(IReadOnlyCollection<FriendRequestResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult>GetIncomingRequests( CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result =await _friendService.GetIncomingRequestsAsync( userId,cancellationToken);

            return Ok(result);
        }

        [HttpGet("requests/outgoing")]
        [ProducesResponseType(
            typeof(IReadOnlyCollection<FriendRequestResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult>GetOutgoingRequests(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result = await _friendService.GetOutgoingRequestsAsync(userId,cancellationToken);

            return Ok(result);
        }

        [HttpPost("requests")]
        [ProducesResponseType(
            typeof(FriendRequestResponseDto),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SendRequest(SendFriendRequestDto request,CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result =await _friendService.SendRequestAsync(userId, request,cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }

        [HttpPost("requests/{requestId:int}/accept")]
        [ProducesResponseType(
            typeof(FriendResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AcceptRequest(
            int requestId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result =
                await _friendService.AcceptRequestAsync( userId,requestId, cancellationToken);

            return Ok(result);
        }

        [HttpPost("requests/{requestId:int}/reject")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectRequest(
            int requestId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _friendService.RejectRequestAsync(
                userId,
                requestId,
                cancellationToken);

            return NoContent();
        }

        [HttpDelete("requests/{requestId:int}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelRequest(
            int requestId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _friendService.CancelRequestAsync(userId,requestId,cancellationToken);

            return NoContent();
        }

        [HttpDelete("{friendUserId:int}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveFriend( int friendUserId,CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _friendService.RemoveFriendAsync(userId,friendUserId,cancellationToken);

            return NoContent();
        }

        [HttpGet("blocked")]
        [ProducesResponseType(
            typeof(IReadOnlyCollection<FriendUserResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBlockedUsers(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result =await _friendService.GetBlockedUsersAsync( userId,cancellationToken);

            return Ok(result);
        }

        [HttpPost("blocked/{blockedUserId:int}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> BlockUser(int blockedUserId,CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _friendService.BlockUserAsync(userId, blockedUserId, cancellationToken);

            return NoContent();
        }

        [HttpDelete("blocked/{blockedUserId:int}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnblockUser(
            int blockedUserId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            await _friendService.UnblockUserAsync(userId,blockedUserId,cancellationToken);

            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var userIdValue =User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedException("Access token etibarsızdır.");
            }

            return userId;
        }
    }
}