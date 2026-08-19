using Microsoft.AspNetCore.Components.Forms;

namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// Core model for a stored file, used by <see cref="IFileStorageProvider"/>.
    /// Modules implement the mapping from their own contracts to this type.
    /// </summary>
    public class StorageFileItem
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public string StorageProvider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Result returned after a successful file upload via <see cref="IFileStorageProvider"/>.
    /// </summary>
    public class StorageUploadResult
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    /// <summary>
    /// Provider interface for file-storage operations (list, upload, download URL, delete).
    /// Define in <c>TaindSoft.AdminUI</c>; implement in <c>StorageManagement.AdminUI</c>;
    /// inject via DI wherever modules need storage without a direct cross-module reference.
    /// </summary>
    public interface IFileStorageProvider
    {
        Task<(List<StorageFileItem> Items, int Total)> ListAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken cancellationToken = default);

        Task<string?> GetFileUrlAsync(int fileId, CancellationToken cancellationToken = default);

        Task<StorageUploadResult?> UploadAsync(
            IBrowserFile file,
            string? container = null,
            string visibility = "Public",
            CancellationToken cancellationToken = default);

        Task DeleteAsync(int fileId, CancellationToken cancellationToken = default);
    }
}
