namespace Discord.Core.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string displayName, string token,
        CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string email, string displayName, string token,
        CancellationToken cancellationToken = default);
}
