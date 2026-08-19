namespace TaindSoft.Core.Infrastructure.Storage
{
    /// <summary>
    /// TODO: Document class LocalFileSystemStorageOptions
    /// </summary>
    public class LocalFileSystemStorageOptions
    {
        public string BasePath { get; set; } = "./storage";
    }

    /// <summary>
    /// TODO: Document class LocalFileSystemStorageProvider
    /// </summary>
    public class LocalFileSystemStorageProvider(LocalFileSystemStorageOptions opts) : IStorageProvider
    {
        private readonly LocalFileSystemStorageOptions _opts = opts ?? throw new ArgumentNullException(nameof(opts));

        public string ProviderName => "LocalFileSystem";

        public async Task<StorageSaveResult> SaveAsync(string storagePath, Stream content, string mimeType, CancellationToken cancellationToken = default)
        {
            string fullPath = Path.Combine(_opts.BasePath, storagePath);
            string? dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }

            using FileStream fs = new(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await content.CopyToAsync(fs, cancellationToken);
            await fs.FlushAsync(cancellationToken);

            FileInfo fi = new(fullPath);
            return new StorageSaveResult(storagePath, fi.Name, fi.Length);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            string fullPath = Path.Combine(_opts.BasePath, storagePath);
            Stream s = File.OpenRead(fullPath);
            return Task.FromResult(s);
        }

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            string fullPath = Path.Combine(_opts.BasePath, storagePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            string fullPath = Path.Combine(_opts.BasePath, storagePath);
            return Task.FromResult(File.Exists(fullPath));
        }

        public Task<string?> GenerateUrlAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            // Local filesystem does not provide public URL by default
            return Task.FromResult<string?>(null);
        }
    }
}
