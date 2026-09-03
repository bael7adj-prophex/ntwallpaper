namespace NTWallpaper.Domain.Interfaces;

using NTWallpaper.Domain.Models;

/// <summary>Abstraction over an external image source (Pixabay, Unsplash, local folder, ...).</summary>
public interface IImageProvider
{
    string Name { get; }

    Task<IReadOnlyList<ImageResult>> SearchAsync(ImageSearchOptions options, CancellationToken cancellationToken);
}

/// <summary>Downloads image bytes from a URL to a local file path.</summary>
public interface IImageDownloadService
{
    Task<string> DownloadAsync(string url, string destinationPath, CancellationToken cancellationToken);
}
