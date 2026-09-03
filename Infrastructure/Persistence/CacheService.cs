namespace NTWallpaper.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>Local image cache: prevents duplicate downloads and tracks usage/history.</summary>
public class CacheService : ICacheService
{
    private readonly IImageRepository _repository;
    private readonly IImageDownloadService _download;
    private readonly ISettingsService _settings;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IImageRepository repository, IImageDownloadService download, ISettingsService settings, ILogger<CacheService> logger)
    {
        _repository = repository;
        _download = download;
        _settings = settings;
        _logger = logger;
    }

    public async Task<CachedImage?> GetOrDownloadAsync(ImageResult result, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByPixabayIdAsync(result.PixabayId, cancellationToken);
        if (existing is not null && File.Exists(existing.LocalPath))
            return existing;

        var directory = _settings.Current.CacheDirectory;
        Directory.CreateDirectory(directory);

        var ext = Path.GetExtension(result.SourceUrl);
        if (string.IsNullOrEmpty(ext) || ext.Length > 5) ext = ".jpg";
        var destination = Path.Combine(directory, $"pixabay_{result.PixabayId}{ext}");

        await _download.DownloadAsync(result.SourceUrl, destination, cancellationToken);

        var image = new CachedImage
        {
            PixabayId = result.PixabayId,
            SourceUrl = result.SourceUrl,
            PreviewUrl = result.PreviewUrl,
            LargeImageUrl = result.LargeImageUrl,
            LocalPath = destination,
            SearchTerm = result.SearchTerm,
            Width = result.Width,
            Height = result.Height,
            ImageType = result.ImageType,
            Orientation = result.Orientation,
            Colors = result.Colors,
            DownloadedAtUtc = DateTime.UtcNow
        };

        await _repository.UpsertAsync(image, cancellationToken);
        return await _repository.GetByPixabayIdAsync(result.PixabayId, cancellationToken);
    }

    public Task<CachedImage?> GetCachedAsync(long pixabayId, CancellationToken cancellationToken)
        => _repository.GetByPixabayIdAsync(pixabayId, cancellationToken);

    public Task<IReadOnlyList<CachedImage>> GetAllAsync(CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);

    public Task MarkUsedAsync(long id, CancellationToken cancellationToken)
        => _repository.MarkUsedAsync(id, cancellationToken);

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var image = await _repository.GetByIdAsync(id, cancellationToken);
        if (image is not null && File.Exists(image.LocalPath))
        {
            try { File.Delete(image.LocalPath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete cached file {Path}", image.LocalPath); }
        }

        await _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<long> GetCacheSizeBytesAsync(CancellationToken cancellationToken)
    {
        var directory = _settings.Current.CacheDirectory;
        if (!Directory.Exists(directory)) return 0;
        long total = 0;
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            try { total += new FileInfo(file).Length; }
            catch { /* ignore */ }
        }

        return total;
    }
}
