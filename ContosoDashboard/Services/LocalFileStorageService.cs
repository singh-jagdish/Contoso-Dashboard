using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class DocumentOptions
{
    public string StorageRoot { get; set; } = "AppData/uploads";
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
    public int ScanQueueCapacity { get; set; } = 100;
    public int ScanMaxAttempts { get; set; } = 3;
    public string ScannerMode { get; set; } = "Local";
}

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    public LocalFileStorageService(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetSection("Documents").Get<DocumentOptions>()?.StorageRoot ?? "AppData/uploads";
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default)
    {
        var segment = projectId?.ToString() ?? "personal";
        var relative = Path.Combine(userId.ToString(), segment, $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        var absolute = GetAbsolutePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        await using var output = new FileStream(absolute, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(output, cancellationToken);
        return relative.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = GetAbsolutePath(relativePath);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = GetAbsolutePath(relativePath);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("Storage paths must be relative.");
        var full = Path.GetFullPath(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage path escapes the private root.");
        return full;
    }
}
