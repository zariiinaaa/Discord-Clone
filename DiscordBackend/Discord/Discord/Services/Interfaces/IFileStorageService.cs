namespace Discord.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveAvatarAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

        void DeleteAvatar(string? avatarUrl);
    }
}
