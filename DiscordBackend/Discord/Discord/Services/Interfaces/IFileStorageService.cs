using Discord.Core.Models.Files;

namespace Discord.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveAvatarAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

        void DeleteAvatar(string? avatarUrl);

        Task<string> SaveServerIconAsync(IFormFile file,CancellationToken cancellationToken = default);

        void DeleteServerIcon(string? iconUrl);

        Task<StoredFileResult> SaveMessageAttachmentAsync(IFormFile file,
        CancellationToken cancellationToken = default);

        void DeleteMessageAttachment(string? fileUrl);
    }
}
