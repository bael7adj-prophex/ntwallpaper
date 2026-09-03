namespace NTWallpaper.Infrastructure.Windows;

using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Enums;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>
/// Produces a rendered wallpaper file. For non-Span styles Windows handles the
/// fill/fit/crop, so the source path is returned unchanged. For Span, a single
/// composed image is generated across the bounding box of all targets.
/// </summary>
public class ImageRenderingService : IImageRenderingService
{
    private readonly ISettingsService _settings;
    private readonly ILogger<ImageRenderingService> _logger;

    public ImageRenderingService(ISettingsService settings, ILogger<ImageRenderingService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> RenderAsync(string sourcePath, IReadOnlyList<WallpaperTarget> targets, WallpaperStyle style, CancellationToken cancellationToken)
    {
        if (style != WallpaperStyle.Span)
            return sourcePath;

        var output = await Task.Run(() => ComposeSpan(sourcePath, targets), cancellationToken);
        return output;
    }

    private string ComposeSpan(string sourcePath, IReadOnlyList<WallpaperTarget> targets)
    {
        var minX = targets.Min(t => t.X);
        var minY = targets.Min(t => t.Y);
        var maxX = targets.Max(t => t.X + t.Width);
        var maxY = targets.Max(t => t.Y + t.Height);
        var canvasW = maxX - minX;
        var canvasH = maxY - minY;

        using var source = Image.FromFile(sourcePath);
        using var canvas = new Bitmap(canvasW, canvasH);
        using (var g = Graphics.FromImage(canvas))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.Clear(Color.Black);

            foreach (var target in targets)
            {
                var rect = new Rectangle(target.X - minX, target.Y - minY, target.Width, target.Height);
                DrawFill(g, source, rect);
            }
        }

        var directory = _settings.Current.CacheDirectory;
        Directory.CreateDirectory(directory);
        var output = Path.Combine(directory, $"span_{canvasW}x{canvasH}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg");
        canvas.Save(output, System.Drawing.Imaging.ImageFormat.Jpeg);
        _logger.LogInformation("Composed span wallpaper {Path}", output);
        return output;
    }

    private static void DrawFill(Graphics g, Image image, Rectangle rect)
    {
        var scale = Math.Max((double)rect.Width / image.Width, (double)rect.Height / image.Height);
        var drawW = image.Width * scale;
        var drawH = image.Height * scale;
        var offsetX = rect.X + (rect.Width - drawW) / 2;
        var offsetY = rect.Y + (rect.Height - drawH) / 2;

        g.Save();
        g.IntersectClip(rect);
        g.DrawImage(image, (float)offsetX, (float)offsetY, (float)drawW, (float)drawH);
        g.Restore();
    }
}
