namespace NTWallpaper.Domain.Models;

/// <summary>A downloaded image persisted in the local cache and history database.</summary>
public class CachedImage
{
    public long Id { get; set; }

    public long PixabayId { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string LargeImageUrl { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;

    public string SearchTerm { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string ImageType { get; set; } = string.Empty;
    public string Orientation { get; set; } = string.Empty;
    public string? Colors { get; set; }

    public DateTime DownloadedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FirstUsedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public int TimesDisplayed { get; set; }

    public bool IsFavorite { get; set; }
    public int Rating { get; set; }
}
