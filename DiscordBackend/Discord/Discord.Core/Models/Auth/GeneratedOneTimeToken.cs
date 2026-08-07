namespace Discord.Core.Models.Auth;

public sealed record GeneratedOneTimeToken(
    string RawToken,
    string TokenHash,
    DateTime ExpiresAt);
