using Microsoft.AspNetCore.Components.Forms;

namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// Lightweight model used by the AdminMediaPicker component.
    /// Modules implementing IMediaPickerService map their domain DTOs to this type.
    /// </summary>
    public class MediaPickerItem
    {
        public long FileSize { get; set; }
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? StoragePath { get; set; }
        public string? StorageProvider { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Service interface for browsing and uploading media from within the AdminMediaPicker component.
    /// Implement in a module's AdminUI layer and register as scoped.
    /// </summary>
    public interface IMediaPickerService
    {
        Task<MediaPickerPageResult> GetPageAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);

        /// <summary>Upload a file and return the resulting media item.</summary>
        Task<MediaPickerItem> UploadAsync(IBrowserFile file, IProgress<int>? progress = null);

        /// <summary>Return a base64 data-URL preview for the given file (images only).</summary>
        Task<string> GetPreviewAsync(IBrowserFile file);
    }

    /// <summary>
    /// TODO: Document class MediaPickerPageResult
    /// </summary>
    public class MediaPickerPageResult
    {
        public List<MediaPickerItem> Items { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
    }
}
