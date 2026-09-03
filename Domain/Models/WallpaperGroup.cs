namespace NTWallpaper.Domain.Models;

using NTWallpaper.Domain.Enums;

/// <summary>
/// A group receives ONE logical wallpaper; every target assigned to the group
/// receives the same wallpaper (possibly rendered differently per target).
/// </summary>
public class WallpaperGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public RotationInterval RotationInterval { get; set; } = RotationInterval.Hour1;
    public TimeSpan? CustomInterval { get; set; }
    public TimeOnly? SpecificTime { get; set; }

    public WallpaperStyle WallpaperStyle { get; set; } = WallpaperStyle.Fill;

    /// <summary>Tag ids (references to <see cref="Tag"/>) used for this group.</summary>
    public List<string> TagIds { get; set; } = new();

    /// <summary>Fallback tag ids used when primary tags yield no result.</summary>
    public List<string> FallbackTagIds { get; set; } = new();

    /// <summary>Next scheduled rotation time in UTC (null = not scheduled).</summary>
    public DateTime? NextRotationUtc { get; set; }

    public AppState State { get; set; } = AppState.Idle;
}
