namespace NTWallpaper.Infrastructure.Windows;

using System.Runtime.InteropServices;

/// <summary>COM interop for the documented <see cref="IDesktopWallpaper"/> (ShObjIdl) interface.</summary>
[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDesktopWallpaper
{
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
    void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, [MarshalAs(UnmanagedType.LPWStr)] out string? wallpaper);
    void GetMonitorDevicePathCount(out uint count);
    void GetMonitorDevicePathAt(uint monitorIndex, [MarshalAs(UnmanagedType.LPWStr)] out string? monitorID);
    void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID, out RECT displayRect);
    void SetBackgroundColor(uint color);
    void GetBackgroundColor(out uint color);
    void SetPosition(int position);
    void GetPosition(out int position);
    void SetSlideshow(IntPtr items);
    void GetSlideshow(out IntPtr items);
    void SetSlideshowOptions(uint options, uint slideshowTick);
    void GetSlideshowOptions(out uint options, out uint slideshowTick);
    void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, int direction);
    void GetStatus(out int status);
    void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

internal static class DesktopWallpaperFactory
{
    private static readonly Guid Clsid = new Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CCAA2");

    public static IDesktopWallpaper Create()
    {
        var type = Type.GetTypeFromCLSID(Clsid);
        if (type is null)
            throw new InvalidOperationException("IDesktopWallpaper coclass is not available on this system.");
        return (IDesktopWallpaper)Activator.CreateInstance(type)!;
    }
}
