namespace Discord.Core.Settings;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Discord Clone";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
    public string VerificationPath { get; set; } = "/verify-email";
    public string PasswordResetPath { get; set; } = "/reset-password";
}
