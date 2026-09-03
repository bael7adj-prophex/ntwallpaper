namespace NTWallpaper.Infrastructure.Windows;

using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>
/// Per-virtual-desktop wallpapers are NOT exposed by any documented, supported
/// Windows API (only undocumented internals exist). We report this limitation
/// honestly rather than faking support.
/// </summary>
public class VirtualDesktopService : IVirtualDesktopService
{
    public bool IsSupported => false;

    public string NotSupportedReason =>
        "Windows does not provide a documented, supported API for per-virtual-desktop wallpapers. " +
        "This feature is disabled to avoid relying on undocumented internals.";

    public IReadOnlyList<WallpaperTarget> GetVirtualDesktops() => Array.Empty<WallpaperTarget>();
}
