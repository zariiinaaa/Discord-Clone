using Discord.Core.DTOs.Auth.Requests;
using Discord.Core.DTOs.Auth.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthOperationResponseDto> RegisterAsync(
       RegisterRequestDto request,
       string? ipAddress,
       CancellationToken cancellationToken = default);

        Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthResponseDto> RefreshTokenAsync(
            RefreshTokenRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken = default);

        Task<AuthOperationResponseDto> VerifyEmailAsync(
            VerifyEmailRequestDto request,
            CancellationToken cancellationToken = default);

        Task<AuthOperationResponseDto> ResendVerificationEmailAsync(
            ResendVerificationEmailRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthOperationResponseDto> ForgotPasswordAsync(
            ForgotPasswordRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthOperationResponseDto> ResetPasswordAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
