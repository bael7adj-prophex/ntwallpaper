namespace NTWallpaper.Domain.Models;

using NTWallpaper.Domain.Enums;

/// <summary>Application-wide settings (persisted, except the API key which is stored securely).</summary>
public class AppSettings
{
    public string Language { get; set; } = "en";
    public string? Category { get; set; }
    public ImageType ImageType { get; set; } = ImageType.All;
    public Orientation Orientation { get; set; } = Orientation.All;
    public string? Colors { get; set; }
    public bool EditorsChoice { get; set; }
    public bool SafeSearch { get; set; } = true;
    public string Order { get; set; } = "popular";
    public int PerPage { get; set; } = 30;
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }

    public int RetryCount { get; set; } = 3;

    public string CacheDirectory { get; set; } = string.Empty;
    public string DatabasePath { get; set; } = string.Empty;

    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    public bool UseGlobalFallbackWhenGroupEmpty { get; set; } = true;

    // Recommendation scoring weights (percentages, should sum to ~100).
    public int WeightResolution { get; set; } = 25;
    public int WeightAspectRatio { get; set; } = 25;
    public int WeightQuality { get; set; } = 15;
    public int WeightRelevance { get; set; } = 15;
    public int WeightPopularity { get; set; } = 10;
    public int WeightEditorsChoice { get; set; } = 5;
    public int WeightNovelty { get; set; } = 5;
}
