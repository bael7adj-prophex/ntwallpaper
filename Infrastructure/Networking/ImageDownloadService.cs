namespace NTWallpaper.Infrastructure.Networking;

using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Interfaces;
using System.IO;
using System.Net.Http;

/// <summary>Downloads an image from a URL and validates it is a real image before returning.</summary>
public class ImageDownloadService : IImageDownloadService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    private readonly ILogger<ImageDownloadService> _logger;

    public ImageDownloadService(ILogger<ImageDownloadService> logger) => _logger = logger;

    public async Task<string> DownloadAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading {Url}", url);
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var fs = File.Create(destinationPath))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }

        ValidateImage(destinationPath);
        return destinationPath;
    }

    private static void ValidateImage(string path)
    {
        using var fs = File.OpenRead(path);
        var header = new byte[8];
        var read = fs.Read(header, 0, header.Length);
        var valid = read >= 2 && (
            (header[0] == 0xFF && header[1] == 0xD8) ||                                   // JPEG
            (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) || // PNG
            (header[0] == 0x42 && header[1] == 0x4D) ||                                   // BMP
            (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46));               // GIF
        if (!valid)
            throw new InvalidDataException("Downloaded file is not a supported image format.");
    }
}