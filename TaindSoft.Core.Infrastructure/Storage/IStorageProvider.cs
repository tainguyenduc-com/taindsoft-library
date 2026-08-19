namespace TaindSoft.Core.Infrastructure.Storage
{
    /// <summary>
    /// TODO: Document interface IStorageProvider
    /// </summary>
    public interface IStorageProvider
    {
        string ProviderName { get; }

        Task<StorageSaveResult> SaveAsync(string storagePath, Stream content, string mimeType, CancellationToken cancellationToken = default);
        Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
        Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);
        Task<string?> GenerateUrlAsync(string storagePath, CancellationToken cancellationToken = default);
    }

    public sealed record StorageSaveResult(string StoragePath, string StoredName, long Size);
}
