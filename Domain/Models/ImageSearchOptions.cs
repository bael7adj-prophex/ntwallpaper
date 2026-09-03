namespace NTWallpaper.Domain.Models;

using NTWallpaper.Domain.Enums;

/// <summary>Options used to query an image provider (Pixabay).</summary>
public class ImageSearchOptions
{
    public string Query { get; set; } = string.Empty;
    public string? Lang { get; set; }
    public string? Category { get; set; }
    public ImageType ImageType { get; set; } = ImageType.All;
    public Orientation Orientation { get; set; } = Orientation.All;
    public int? MinWidth { get; set; }
    public int? MinHeight { get; set; }
    public string? Colors { get; set; }
    public bool EditorsChoice { get; set; }
    public bool SafeSearch { get; set; } = true;
    public string Order { get; set; } = "popular";
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 30;
}
