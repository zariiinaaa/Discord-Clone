using Discord.Core.Enums;
using Discord.Core.Interfaces;
using Discord.Core.Models.Auth;
using Discord.Core.Settings;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Discord.Infrastructure.Services;

public class OneTimeTokenService : IOneTimeTokenService
{
    private readonly AuthTokenSettings _settings;

    public OneTimeTokenService(IOptions<AuthTokenSettings> options)
    {
        _settings = options.Value;
    }

    public GeneratedOneTimeToken Generate(AuthTokenPurpose purpose)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var expiresAt = purpose switch
        {
            AuthTokenPurpose.EmailVerification =>
                DateTime.UtcNow.AddHours(_settings.EmailVerificationExpirationHours),
            AuthTokenPurpose.PasswordReset =>
                DateTime.UtcNow.AddMinutes(_settings.PasswordResetExpirationMinutes),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose))
        };

        return new GeneratedOneTimeToken(rawToken, HashToken(rawToken), expiresAt);
    }

    public string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
