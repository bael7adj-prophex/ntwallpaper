namespace NTWallpaper.Domain.Models;

using NTWallpaper.Domain.Enums;

/// <summary>
/// A location where a wallpaper can be applied. Targets are discovered from the
/// live Windows environment (monitors, virtual desktops, lock screen).
/// </summary>
public class WallpaperTarget
{
    /// <summary>Stable identifier (monitor device path, virtual desktop id, or "lockscreen").</summary>
    public string Id { get; set; } = string.Empty;

    public TargetKind Kind { get; set; } = TargetKind.Monitor;

    public string DisplayName { get; set; } = string.Empty;

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public double DpiX { get; set; } = 96.0;
    public double DpiY { get; set; } = 96.0;

    public bool IsPrimary { get; set; }

    /// <summary>Group this target currently belongs to (null if unassigned).</summary>
    public string? GroupId { get; set; }

    public double AspectRatio => Width > 0 && Height > 0 ? (double)Width / Height : 1.0;
}
