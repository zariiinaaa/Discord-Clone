using Discord.Core.Exceptions;
using Discord.Core.Models.Files;
using Discord.Services.Interfaces;

namespace Discord.Services
{
    public class LocalFileStorageService : IFileStorageService
    {

        private const long MaximumFileSize = 5 * 1024 * 1024;
        private const long MaximumMessageFileSize = 25 * 1024 * 1024;


        private readonly IWebHostEnvironment _environment;

        private static readonly Dictionary<string, string[]>
            AllowedFileTypes = new(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = ["image/jpeg"],
                [".jpeg"] = ["image/jpeg"],
                [".png"] = ["image/png"],
                [".webp"] = ["image/webp"]
            };
        private static readonly Dictionary<string, string[]>
    AllowedMessageFileTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".webp"] = ["image/webp"],
            [".gif"] = ["image/gif"],
            [".pdf"] = ["application/pdf"],
            [".docx"] =
            [
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            ],
            [".zip"] =
            [
                "application/zip",
                "application/x-zip-compressed"
            ]
        };

        public LocalFileStorageService(
            IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAvatarAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw new BadRequestException(
                    "Avatar faylı seçilməyib.");
            }

            if (file.Length > MaximumFileSize)
            {
                throw new BadRequestException(
                    "Avatar maksimum 5 MB ola bilər.");
            }

            var extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!AllowedFileTypes.TryGetValue(
                    extension,
                    out var allowedContentTypes))
            {
                throw new BadRequestException(
                    "Yalnız JPG, JPEG, PNG və WEBP faylları qəbul edilir.");
            }

            if (!allowedContentTypes.Contains(
                    file.ContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException(
                    "Faylın content type məlumatı düzgün deyil.");
            }

            await ValidateFileSignatureAsync(
                file,
                extension,
                cancellationToken);

            var webRootPath = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var avatarDirectory = Path.Combine(
                webRootPath,
                "uploads",
                "avatars");

            Directory.CreateDirectory(avatarDirectory);

            var safeFileName =
                $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(
                avatarDirectory,
                safeFileName);

            await using var outputStream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(
                outputStream,
                cancellationToken);

            return $"/uploads/avatars/{safeFileName}";
        }

        public void DeleteAvatar(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl) ||
                !avatarUrl.StartsWith(
                    "/uploads/avatars/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var safeFileName = Path.GetFileName(avatarUrl);

            var webRootPath = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var physicalPath = Path.Combine(
                webRootPath,
                "uploads",
                "avatars",
                safeFileName);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        private static async Task ValidateFileSignatureAsync(
            IFormFile file,
            string extension,
            CancellationToken cancellationToken)
        {
            var header = new byte[12];

            await using var stream = file.OpenReadStream();

            var bytesRead = await stream.ReadAsync(
                header.AsMemory(),
                cancellationToken);

            if (!HasValidSignature(
                    extension,
                    header.AsSpan(0, bytesRead)))
            {
                throw new BadRequestException(
                    "Faylın real formatı düzgün deyil.");
            }
        }

        public async Task<string> SaveServerIconAsync(IFormFile file,CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw new BadRequestException(
                    "Server icon faylı seçilməyib.");
            }

            if (file.Length > MaximumFileSize)
            {
                throw new BadRequestException(
                    "Server icon maksimum 5 MB ola bilər.");
            }

            var extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!AllowedFileTypes.TryGetValue(
                    extension,
                    out var allowedContentTypes))
            {
                throw new BadRequestException(
                    "Yalnız JPG, JPEG, PNG və WEBP faylları qəbul edilir.");
            }

            if (!allowedContentTypes.Contains(
                    file.ContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException(
                    "Faylın content type məlumatı düzgün deyil.");
            }

            await ValidateFileSignatureAsync( file,extension,
                cancellationToken);

            var webRootPath =
                _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var iconDirectory = Path.Combine(
                webRootPath,
                "uploads",
                "server-icons");

            Directory.CreateDirectory( iconDirectory);

            var safeFileName = $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(
                iconDirectory,
                safeFileName);

            await using var outputStream =
                new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await file.CopyToAsync(
                outputStream,
                cancellationToken);

            return
                $"/uploads/server-icons/{safeFileName}";
        }

        public void DeleteServerIcon(string? iconUrl)
        {
            if (
                string.IsNullOrWhiteSpace(iconUrl) ||
                !iconUrl.StartsWith(
                    "/uploads/server-icons/",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return;
            }

            var safeFileName =
                Path.GetFileName(iconUrl);

            var webRootPath =
                _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var physicalPath = Path.Combine(
                webRootPath,
                "uploads",
                "server-icons",
                safeFileName);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        public async Task<StoredFileResult>
    SaveMessageAttachmentAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw new BadRequestException(
                    "Mesaj faylı seçilməyib.");
            }

            if (file.Length > MaximumMessageFileSize)
            {
                throw new BadRequestException(
                    "Mesaj faylı maksimum 25 MB ola bilər.");
            }

            var originalFileName =
                Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(originalFileName) ||
                originalFileName.Length > 255)
            {
                throw new BadRequestException(
                    "Fayl adı düzgün deyil.");
            }

            var extension = Path
                .GetExtension(originalFileName)
                .ToLowerInvariant();

            if (!AllowedMessageFileTypes.TryGetValue(
                    extension,
                    out var allowedContentTypes))
            {
                throw new BadRequestException(
                    "Bu fayl formatı dəstəklənmir.");
            }

            if (!allowedContentTypes.Contains(
                    file.ContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException(
                    "Faylın content type məlumatı düzgün deyil.");
            }

            await ValidateFileSignatureAsync(
                file,
                extension,
                cancellationToken);

            var webRootPath =
                _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var attachmentDirectory = Path.Combine(
                webRootPath,
                "uploads",
                "messages");

            Directory.CreateDirectory(
                attachmentDirectory);

            var storedFileName =
                $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(
                attachmentDirectory,
                storedFileName);

            await using var outputStream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(
                outputStream,
                cancellationToken);

            return new StoredFileResult(
                originalFileName,
                storedFileName,
                $"/uploads/messages/{storedFileName}",
                file.ContentType,
                file.Length);
        }

        public void DeleteMessageAttachment(string? fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl) ||
                !fileUrl.StartsWith(
                    "/uploads/messages/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var storedFileName =Path.GetFileName(fileUrl);

            var webRootPath = _environment.WebRootPath?? Path.Combine(
             _environment.ContentRootPath,"wwwroot");

            var physicalPath = Path.Combine(
                webRootPath,
                "uploads",
                "messages",
                storedFileName);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        private static bool HasValidSignature(string extension,
        ReadOnlySpan<byte> header)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" =>
                    header.Length >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                ".png" =>
                    header.Length >= 8 &&
                    header[..8].SequenceEqual(
                        new byte[]
                        {
                    0x89, 0x50, 0x4E, 0x47,
                    0x0D, 0x0A, 0x1A, 0x0A
                        }),

                ".webp" =>
                    header.Length >= 12 &&
                    header[..4].SequenceEqual("RIFF"u8) &&
                    header.Slice(8, 4).SequenceEqual("WEBP"u8),

                ".gif" =>
                    header.Length >= 6 &&
                    (header[..6].SequenceEqual("GIF87a"u8) ||
                     header[..6].SequenceEqual("GIF89a"u8)),

                ".pdf" =>
                    header.Length >= 5 &&
                    header[..5].SequenceEqual("%PDF-"u8),

                ".docx" or ".zip" =>
                    header.Length >= 4 &&
                    header[0] == 0x50 &&
                    header[1] == 0x4B &&
                    ((header[2] == 0x03 && header[3] == 0x04) ||
                     (header[2] == 0x05 && header[3] == 0x06) ||
                     (header[2] == 0x07 && header[3] == 0x08)),

                _ => false
            };
        }
    }
}
