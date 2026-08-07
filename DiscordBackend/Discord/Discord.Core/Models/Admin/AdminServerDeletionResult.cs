namespace Discord.Core.Models.Admin;

public sealed record AdminServerDeletionResult(
    int ServerId,
    string? IconUrl,
    IReadOnlyCollection<string> AttachmentUrls,
    IReadOnlyCollection<int> MemberUserIds);
