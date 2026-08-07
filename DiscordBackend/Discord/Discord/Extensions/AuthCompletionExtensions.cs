using Discord.Authorization;
using Discord.Core.Interfaces;
using Discord.Core.Settings;
using Discord.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Discord.Extensions;

public static class AuthCompletionExtensions
{
    public static IServiceCollection AddAuthCompletion(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuthTokenSettings>()
            .Bind(configuration.GetSection("AuthTokenSettings"))
            .Validate(settings => settings.EmailVerificationExpirationHours > 0)
            .Validate(settings => settings.PasswordResetExpirationMinutes > 0)
            .Validate(settings => settings.RequestCooldownSeconds > 0)
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("EmailSettings"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Host), "SMTP host is required.")
            .Validate(settings => settings.Port is > 0 and <= 65535, "SMTP port is invalid.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Username), "SMTP username is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Password), "SMTP password is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.FromEmail), "Sender email is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.FrontendBaseUrl), "Frontend URL is required.")
            .ValidateOnStart();

        services.AddScoped<IOneTimeTokenService, OneTimeTokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Çox sayda sorğu göndərildi. Daha sonra yenidən cəhd edin." },
                    cancellationToken);
            };
            options.AddPolicy(AuthRateLimitPolicyNames.EmailRequest, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    }));
            options.AddPolicy(AuthRateLimitPolicyNames.TokenConsumption, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
