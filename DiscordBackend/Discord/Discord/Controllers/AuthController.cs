using Discord.Core.DTOs.Auth.Requests;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Discord.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController:ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(
                request,
                GetIpAddress(),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(
                request,
                GetIpAddress(),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshTokenAsync(
                request,
                GetIpAddress(),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(
                request,
                cancellationToken);

            return NoContent();
        }

        private string? GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
