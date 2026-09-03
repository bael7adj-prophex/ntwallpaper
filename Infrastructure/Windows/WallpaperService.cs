namespace NTWallpaper.Infrastructure.Windows;

using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Enums;
using NTWallpaper.Domain.Interfaces;

/// <summary>Applies wallpapers using the documented IDesktopWallpaper COM API.</summary>
public class WallpaperService : IWallpaperService
{
    private readonly ILogger<WallpaperService> _logger;

    public WallpaperService(ILogger<WallpaperService> logger) => _logger = logger;

    public async Task ApplyWallpaperAsync(string imagePath, WallpaperStyle style, IEnumerable<string> monitorIds, CancellationToken cancellationToken)
    {
        var ids = monitorIds.ToList();
        await Task.Run(() =>
        {
            var wallpaper = DesktopWallpaperFactory.Create();
            try
            {
                if (style == WallpaperStyle.Span)
                {
                    wallpaper.SetPosition((int)WallpaperStyle.Span);
                    wallpaper.SetWallpaper("*", imagePath);
                }
                else
                {
                    wallpaper.SetPosition((int)style);
                    foreach (var id in ids)
                        wallpaper.SetWallpaper(id, imagePath);
                }

                _logger.LogInformation("Applied wallpaper to {Count} target(s) with style {Style}.", ids.Count, style);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply wallpaper.");
                throw;
            }
        }, cancellationToken);
    }

    public async Task ApplyToAllAsync(string imagePath, WallpaperStyle style, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var wallpaper = DesktopWallpaperFactory.Create();
            wallpaper.SetPosition((int)style);
            wallpaper.SetWallpaper("*", imagePath);
            _logger.LogInformation("Applied wallpaper to all monitors with style {Style}.", style);
        }, cancellationToken);
    }
}
