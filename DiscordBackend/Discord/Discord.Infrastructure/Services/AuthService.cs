using Discord.Core.DTOs.Auth.Requests;
using Discord.Core.DTOs.Auth.Responses;
using Discord.Core.Entities;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Discord.Application.Mappings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Services
{
    public class AuthService: IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IValidator<RegisterRequestDto> _registerValidator;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<RefreshTokenRequestDto> _refreshTokenValidator;

        public AuthService(
            AppDbContext dbContext,
            ITokenService tokenService,
            IPasswordHasher<User> passwordHasher,
            IValidator<RegisterRequestDto> registerValidator,
            IValidator<LoginRequestDto> loginValidator,
            IValidator<RefreshTokenRequestDto> refreshTokenValidator)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _refreshTokenValidator = refreshTokenValidator;
        }

        public async Task<AuthResponseDto> RegisterAsync(
            RegisterRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(
                _registerValidator,
                request,
                cancellationToken);

            var email = request.Email.Trim().ToLowerInvariant();
            var username = request.Username.Trim();

            var emailExists = await _dbContext.Users
                .AnyAsync(
                    user => user.Email == email,
                    cancellationToken);

            if (emailExists)
            {
                throw new ConflictException(
                    "Bu email artıq istifadə olunur.");
            }

            var usernameExists = await _dbContext.Users
                .AnyAsync(
                    user => user.Username == username,
                    cancellationToken);

            if (usernameExists)
            {
                throw new ConflictException(
                    "Bu username artıq istifadə olunur.");
            }

            var user = new User
            {
                Username = username,
                DisplayName = request.DisplayName.Trim(),
                Email = email,
                Status = UserStatus.Online,
                Role = PlatformRole.User,
                LastSeenAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);

            _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await CreateAuthResponseAsync(
                user,
                ipAddress,
                cancellationToken);
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(
                _loginValidator,
                request,
                cancellationToken);

            var identifier = request.EmailOrUsername.Trim();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(
                    currentUser =>
                        currentUser.Email == identifier.ToLowerInvariant()
                        || currentUser.Username == identifier,
                    cancellationToken);

            if (user is null)
            {
                throw new UnauthorizedException(
                    "Email, username və ya password yanlışdır.");
            }

            EnsureUserCanLogin(user);

            var verificationResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException(
                    "Email, username və ya password yanlışdır.");
            }

            if (verificationResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    request.Password);
            }

            user.Status = UserStatus.Online;
            user.LastSeenAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            return await CreateAuthResponseAsync(
                user,
                ipAddress,
                cancellationToken);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(
            RefreshTokenRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(
                _refreshTokenValidator,
                request,
                cancellationToken);

            var tokenHash = _tokenService.HashToken(
                request.RefreshToken);

            var storedToken = await _dbContext.RefreshTokens
                .Include(refreshToken => refreshToken.User)
                .FirstOrDefaultAsync(
                    refreshToken =>
                        refreshToken.TokenHash == tokenHash,
                    cancellationToken);

            var now = DateTime.UtcNow;

            if (storedToken is null
                || storedToken.RevokedAt is not null
                || storedToken.ExpiresAt <= now)
            {
                throw new UnauthorizedException(
                    "Refresh token etibarsızdır və ya vaxtı bitib.");
            }

            EnsureUserCanLogin(storedToken.User);

            var newRefreshToken =
                _tokenService.GenerateRefreshToken();

            var newRefreshTokenHash =
                _tokenService.HashToken(newRefreshToken);

            storedToken.RevokedAt = now;
            storedToken.ReplacedByTokenHash =
                newRefreshTokenHash;
            storedToken.UpdatedAt = now;

            var refreshTokenEntity = new RefreshToken
            {
                TokenHash = newRefreshTokenHash,
                ExpiresAt =
                    _tokenService.GetRefreshTokenExpiration(),
                CreatedByIp = ipAddress,
                UserId = storedToken.UserId,
                CreatedAt = now
            };

            _dbContext.RefreshTokens.Add(refreshTokenEntity);

            storedToken.User.Status = UserStatus.Online;
            storedToken.User.LastSeenAt = now;
            storedToken.User.UpdatedAt = now;

            var accessToken = _tokenService.GenerateAccessToken(
                storedToken.User);

            var accessTokenExpiration =
                _tokenService.GetAccessTokenExpiration();

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = accessTokenExpiration,
                User = storedToken.User.ToResponseDto()
            };
        }

        public async Task LogoutAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(
                _refreshTokenValidator,
                request,
                cancellationToken);

            var tokenHash = _tokenService.HashToken(
                request.RefreshToken);

            var storedToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(
                    refreshToken =>
                        refreshToken.TokenHash == tokenHash,
                    cancellationToken);

            if (storedToken is null
                || storedToken.RevokedAt is not null)
            {
                return;
            }

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<AuthResponseDto> CreateAuthResponseAsync(
            User user,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var accessToken =
                _tokenService.GenerateAccessToken(user);

            var refreshToken =
                _tokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                TokenHash = _tokenService.HashToken(refreshToken),
                ExpiresAt =
                    _tokenService.GetRefreshTokenExpiration(),
                CreatedByIp = ipAddress,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.RefreshTokens.Add(refreshTokenEntity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt =
                    _tokenService.GetAccessTokenExpiration(),
                User = user.ToResponseDto()
            };
        }

        private static async Task ValidateAsync<TRequest>(
            IValidator<TRequest> validator,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(
                request,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    validationResult.Errors);
            }
        }

        private static void EnsureUserCanLogin(User user)
        {
            if (!user.IsBanned)
            {
                return;
            }

            if (user.BannedUntil.HasValue
                && user.BannedUntil.Value <= DateTime.UtcNow)
            {
                user.IsBanned = false;
                user.BanReason = null;
                user.BannedUntil = null;
                return;
            }

            throw new ForbiddenException(
                user.BanReason ?? "Bu hesab bloklanıb.");
        }
    }
}
