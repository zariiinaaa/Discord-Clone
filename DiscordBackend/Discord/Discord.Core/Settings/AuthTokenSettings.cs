namespace Discord.Core.Settings;

public class AuthTokenSettings
{
    public int EmailVerificationExpirationHours { get; set; } = 24;
    public int PasswordResetExpirationMinutes { get; set; } = 30;
    public int RequestCooldownSeconds { get; set; } = 60;
}
