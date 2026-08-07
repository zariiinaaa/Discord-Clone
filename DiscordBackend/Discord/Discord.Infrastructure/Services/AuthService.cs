using Discord.Application.Mappings;
using Discord.Core.DTOs.Auth.Requests;
using Discord.Core.DTOs.Auth.Responses;
using Discord.Core.Entities;
using Discord.Core.Entities.Privacy;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Core.Settings;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Services
{
    public class AuthService: IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IOneTimeTokenService _oneTimeTokenService;
        private readonly IEmailService _emailService;
        private readonly AuthTokenSettings _authTokenSettings;
        private readonly ILogger<AuthService> _logger;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IValidator<RegisterRequestDto> _registerValidator;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<RefreshTokenRequestDto> _refreshTokenValidator;
        private readonly IValidator<VerifyEmailRequestDto> _verifyEmailValidator;
        private readonly IValidator<ResendVerificationEmailRequestDto> _resendVerificationValidator;
        private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
        private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;

        public AuthService(
            AppDbContext dbContext,
            ITokenService tokenService,
            IOneTimeTokenService oneTimeTokenService,
            IEmailService emailService,
            IPasswordHasher<User> passwordHasher,
            IValidator<RegisterRequestDto> registerValidator,
            IValidator<LoginRequestDto> loginValidator,
            IValidator<RefreshTokenRequestDto> refreshTokenValidator,
            IValidator<VerifyEmailRequestDto> verifyEmailValidator,
            IValidator<ResendVerificationEmailRequestDto> resendVerificationValidator,
            IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
            IValidator<ResetPasswordRequestDto> resetPasswordValidator,
            IOptions<AuthTokenSettings> authTokenOptions,
            ILogger<AuthService> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _oneTimeTokenService = oneTimeTokenService;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _refreshTokenValidator = refreshTokenValidator;
            _verifyEmailValidator = verifyEmailValidator;
            _resendVerificationValidator = resendVerificationValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _authTokenSettings = authTokenOptions.Value;
            _logger = logger;
        }

        public async Task<AuthOperationResponseDto> RegisterAsync(
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
                Status = UserStatus.Offline,
                Role = PlatformRole.User,

                PrivacySettings = new UserPrivacySettings
                {
                    AllowDirectMessagesFromServerMembers = true,
                    EnableMessageRequests = true
                }
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);

            _dbContext.Users.Add(user);

            var generatedToken = _oneTimeTokenService.Generate(
                AuthTokenPurpose.EmailVerification);
            _dbContext.AuthOneTimeTokens.Add(new AuthOneTimeToken
            {
                User = user,
                Purpose = AuthTokenPurpose.EmailVerification,
                TokenHash = generatedToken.TokenHash,
                ExpiresAt = generatedToken.ExpiresAt,
                CreatedByIp = ipAddress
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _emailService.SendVerificationEmailAsync(
                user.Email, user.DisplayName, generatedToken.RawToken, cancellationToken);

            return new AuthOperationResponseDto
            {
                Message = "Qeydiyyat tamamlandı. Email təsdiq linki göndərildi."
            };
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

            EnsureEmailVerified(user);

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
            EnsureEmailVerified(storedToken.User);

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

        public async Task<AuthOperationResponseDto> VerifyEmailAsync(
            VerifyEmailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(_verifyEmailValidator, request, cancellationToken);

            var now = DateTime.UtcNow;
            var tokenHash = _oneTimeTokenService.HashToken(request.Token);
            var token = await _dbContext.AuthOneTimeTokens
                .AsNoTracking()
                .Include(item => item.User)
                .FirstOrDefaultAsync(item =>
                    item.TokenHash == tokenHash &&
                    item.Purpose == AuthTokenPurpose.EmailVerification,
                    cancellationToken);

            if (token is null || token.UsedAt.HasValue || token.ExpiresAt <= now ||
                token.User.EmailVerifiedAt.HasValue)
            {
                throw new BadRequestException("Təsdiq tokeni etibarsızdır və ya vaxtı bitib.");
            }

            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            var consumed = await _dbContext.AuthOneTimeTokens
                .Where(item => item.Id == token.Id && item.UsedAt == null && item.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.UsedAt, now)
                    .SetProperty(item => item.UpdatedAt, now), cancellationToken);

            if (consumed != 1)
            {
                throw new BadRequestException("Təsdiq tokeni etibarsızdır və ya artıq istifadə olunub.");
            }

            var user = await _dbContext.Users.FirstAsync(
                user => user.Id == token.UserId, cancellationToken);
            if (user.EmailVerifiedAt.HasValue)
            {
                throw new BadRequestException("Email artıq təsdiqlənib.");
            }

            user.EmailVerifiedAt = now;
            user.UpdatedAt = now;

            await _dbContext.AuthOneTimeTokens
                .Where(item => item.UserId == user.Id &&
                    item.Purpose == AuthTokenPurpose.EmailVerification &&
                    item.UsedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.UsedAt, now)
                    .SetProperty(item => item.UpdatedAt, now), cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AuthOperationResponseDto { Message = "Email uğurla təsdiqləndi." };
        }

        public async Task<AuthOperationResponseDto> ResendVerificationEmailAsync(
            ResendVerificationEmailRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(_resendVerificationValidator, request, cancellationToken);
            var genericResponse = new AuthOperationResponseDto
            {
                Message = "Uyğun hesab mövcuddursa, təsdiq emaili göndəriləcək."
            };

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users.FirstOrDefaultAsync(
                user => user.Email == email, cancellationToken);
            if (user is null || user.EmailVerifiedAt.HasValue)
            {
                return genericResponse;
            }

            if (await IsInCooldownAsync(user.Id, AuthTokenPurpose.EmailVerification, cancellationToken))
            {
                return genericResponse;
            }

            var generatedToken = await ReplaceTokenAsync(
                user.Id, AuthTokenPurpose.EmailVerification, ipAddress, cancellationToken);

            try
            {
                await _emailService.SendVerificationEmailAsync(
                    user.Email, user.DisplayName, generatedToken, cancellationToken);
            }
            catch (EmailDeliveryException exception)
            {
                _logger.LogError(exception, "Verification email delivery failed for UserId {UserId}.", user.Id);
            }

            return genericResponse;
        }

        public async Task<AuthOperationResponseDto> ForgotPasswordAsync(
            ForgotPasswordRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(_forgotPasswordValidator, request, cancellationToken);
            var genericResponse = new AuthOperationResponseDto
            {
                Message = "Uyğun hesab mövcuddursa, password sıfırlama emaili göndəriləcək."
            };

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users.FirstOrDefaultAsync(
                user => user.Email == email, cancellationToken);
            if (user is null ||
                await IsInCooldownAsync(user.Id, AuthTokenPurpose.PasswordReset, cancellationToken))
            {
                return genericResponse;
            }

            var generatedToken = await ReplaceTokenAsync(
                user.Id, AuthTokenPurpose.PasswordReset, ipAddress, cancellationToken);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(
                    user.Email, user.DisplayName, generatedToken, cancellationToken);
            }
            catch (EmailDeliveryException exception)
            {
                _logger.LogError(exception, "Password reset email delivery failed for UserId {UserId}.", user.Id);
            }

            return genericResponse;
        }

        public async Task<AuthOperationResponseDto> ResetPasswordAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken = default)
        {
            await ValidateAsync(_resetPasswordValidator, request, cancellationToken);

            var now = DateTime.UtcNow;
            var tokenHash = _oneTimeTokenService.HashToken(request.Token);
            var token = await _dbContext.AuthOneTimeTokens.AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.TokenHash == tokenHash && item.Purpose == AuthTokenPurpose.PasswordReset,
                    cancellationToken);

            if (token is null || token.UsedAt.HasValue || token.ExpiresAt <= now)
            {
                throw new BadRequestException("Password sıfırlama tokeni etibarsızdır və ya vaxtı bitib.");
            }

            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            var consumed = await _dbContext.AuthOneTimeTokens
                .Where(item => item.Id == token.Id && item.UsedAt == null && item.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.UsedAt, now)
                    .SetProperty(item => item.UpdatedAt, now), cancellationToken);

            if (consumed != 1)
            {
                throw new BadRequestException("Password sıfırlama tokeni artıq istifadə olunub.");
            }

            var user = await _dbContext.Users.FirstAsync(
                user => user.Id == token.UserId, cancellationToken);
            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.UpdatedAt = now;

            await _dbContext.RefreshTokens
                .Where(refreshToken => refreshToken.UserId == user.Id && refreshToken.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(refreshToken => refreshToken.RevokedAt, now)
                    .SetProperty(refreshToken => refreshToken.UpdatedAt, now), cancellationToken);

            await _dbContext.AuthOneTimeTokens
                .Where(item => item.UserId == user.Id &&
                    item.Purpose == AuthTokenPurpose.PasswordReset && item.UsedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.UsedAt, now)
                    .SetProperty(item => item.UpdatedAt, now), cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AuthOperationResponseDto { Message = "Password uğurla yeniləndi." };
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

        private async Task<bool> IsInCooldownAsync(
            int userId,
            AuthTokenPurpose purpose,
            CancellationToken cancellationToken)
        {
            var cooldownStart = DateTime.UtcNow.AddSeconds(-_authTokenSettings.RequestCooldownSeconds);
            return await _dbContext.AuthOneTimeTokens.AsNoTracking().AnyAsync(
                token => token.UserId == userId && token.Purpose == purpose &&
                    token.CreatedAt >= cooldownStart,
                cancellationToken);
        }

        private async Task<string> ReplaceTokenAsync(
            int userId,
            AuthTokenPurpose purpose,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            await _dbContext.AuthOneTimeTokens
                .Where(token => token.UserId == userId && token.Purpose == purpose && token.UsedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.UsedAt, now)
                    .SetProperty(token => token.UpdatedAt, now), cancellationToken);

            var generatedToken = _oneTimeTokenService.Generate(purpose);
            _dbContext.AuthOneTimeTokens.Add(new AuthOneTimeToken
            {
                UserId = userId,
                Purpose = purpose,
                TokenHash = generatedToken.TokenHash,
                ExpiresAt = generatedToken.ExpiresAt,
                CreatedByIp = ipAddress,
                CreatedAt = now
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return generatedToken.RawToken;
        }

        private static void EnsureEmailVerified(User user)
        {
            if (!user.EmailVerifiedAt.HasValue)
            {
                throw new ForbiddenException("Email ünvanı təsdiqlənməyib.");
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
