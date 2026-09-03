namespace NTWallpaper.Domain.Interfaces;

using NTWallpaper.Domain.Enums;
using NTWallpaper.Domain.Models;

/// <summary>Discovers wallpaper targets from the live Windows environment.</summary>
public interface IMonitorService
{
    IReadOnlyList<WallpaperTarget> GetTargets();

    event EventHandler<TargetsChangedEventArgs>? TargetsChanged;
}

/// <summary>Applies wallpapers to Windows using documented APIs (IDesktopWallpaper).</summary>
public interface IWallpaperService
{
    Task ApplyWallpaperAsync(string imagePath, WallpaperStyle style, IEnumerable<string> monitorIds, CancellationToken cancellationToken);

    Task ApplyToAllAsync(string imagePath, WallpaperStyle style, CancellationToken cancellationToken);
}

/// <summary>Lock-screen integration. Reports real Windows support limitations.</summary>
public interface ILockScreenService
{
    bool IsSupported { get; }

    Task<LockScreenResult> SetLockScreenAsync(string imagePath, CancellationToken cancellationToken);
}

/// <summary>Virtual-desktop integration. Reports real Windows support limitations.</summary>
public interface IVirtualDesktopService
{
    bool IsSupported { get; }

    string NotSupportedReason { get; }

    IReadOnlyList<WallpaperTarget> GetVirtualDesktops();
}

/// <summary>Produces a rendered wallpaper file for a set of targets and a style.</summary>
public interface IImageRenderingService
{
    Task<string> RenderAsync(string sourcePath, IReadOnlyList<WallpaperTarget> targets, WallpaperStyle style, CancellationToken cancellationToken);
}
