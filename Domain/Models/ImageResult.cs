namespace NTWallpaper.Domain.Models;

/// <summary>A single candidate image returned by an <see cref="Domain.Interfaces.IImageProvider"/>.</summary>
public class ImageResult
{
    public long PixabayId { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string LargeImageUrl { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string SearchTerm { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string ImageType { get; set; } = string.Empty;
    public string Orientation { get; set; } = string.Empty;
    public string? Colors { get; set; }
    public int Views { get; set; }
    public int Downloads { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
    public int Favorites { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
}
