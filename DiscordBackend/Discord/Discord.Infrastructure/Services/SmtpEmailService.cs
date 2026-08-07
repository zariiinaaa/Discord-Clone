using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace Discord.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public Task SendVerificationEmailAsync(string email, string displayName, string token,
        CancellationToken cancellationToken = default)
    {
        var link = BuildLink(_settings.VerificationPath, token);
        return SendAsync(email, "Email ünvanınızı təsdiqləyin",
            $"Salam {displayName}, email ünvanınızı təsdiqləmək üçün bu linkdən istifadə edin: {link}",
            $"<p>Salam {WebUtility.HtmlEncode(displayName)},</p><p>Email ünvanınızı təsdiqləmək üçün " +
            $"<a href=\"{WebUtility.HtmlEncode(link)}\">buraya klikləyin</a>.</p>", cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(string email, string displayName, string token,
        CancellationToken cancellationToken = default)
    {
        var link = BuildLink(_settings.PasswordResetPath, token);
        return SendAsync(email, "Password sıfırlama",
            $"Salam {displayName}, password sıfırlamaq üçün bu linkdən istifadə edin: {link}",
            $"<p>Salam {WebUtility.HtmlEncode(displayName)},</p><p>Password sıfırlamaq üçün " +
            $"<a href=\"{WebUtility.HtmlEncode(link)}\">buraya klikləyin</a>.</p>", cancellationToken);
    }

    private async Task SendAsync(string recipient, string subject, string text, string html,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new BodyBuilder { TextBody = text, HtmlBody = html }.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EmailDeliveryException("Email göndərilə bilmədi.", exception);
        }
    }

    private string BuildLink(string path, string token)
    {
        return $"{_settings.FrontendBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}?token={Uri.EscapeDataString(token)}";
    }
}
