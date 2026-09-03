namespace NTWallpaper.Infrastructure.Windows;

using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>Discovers physical monitors and the lock screen as wallpaper targets.</summary>
public class MonitorService : IMonitorService, IDisposable
{
    private readonly ILogger<MonitorService> _logger;

    public MonitorService(ILogger<MonitorService> logger)
    {
        _logger = logger;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public IReadOnlyList<WallpaperTarget> GetTargets()
    {
        var targets = new List<WallpaperTarget>();
        var index = 0;
        foreach (var screen in Screen.AllScreens)
        {
            index++;
            GetMonitorDpi(screen.DeviceName, out var dpiX, out var dpiY);
            targets.Add(new WallpaperTarget
            {
                Id = screen.DeviceName,
                Kind = TargetKind.Monitor,
                DisplayName = screen.Primary ? $"Display {index} (Primary)" : $"Display {index}",
                X = screen.Bounds.X,
                Y = screen.Bounds.Y,
                Width = screen.Bounds.Width,
                Height = screen.Bounds.Height,
                DpiX = dpiX,
                DpiY = dpiY,
                IsPrimary = screen.Primary
            });
        }

        // Lock screen is a special, always-present target.
        var primary = Screen.PrimaryScreen;
        targets.Add(new WallpaperTarget
        {
            Id = "lockscreen",
            Kind = TargetKind.LockScreen,
            DisplayName = "Windows Lock Screen",
            Width = primary?.Bounds.Width ?? 1920,
            Height = primary?.Bounds.Height ?? 1080,
            IsPrimary = false
        });

        return targets;
    }

    public event EventHandler<TargetsChangedEventArgs>? TargetsChanged;

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("Display settings changed; refreshing targets.");
        TargetsChanged?.Invoke(this, new TargetsChangedEventArgs { Targets = GetTargets() });
    }

    private static void GetMonitorDpi(string deviceName, out double dpiX, out double dpiY)
    {
        dpiX = 96.0;
        dpiY = 96.0;
        try
        {
            var hdc = CreateDC(null, deviceName, null, IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                dpiX = GetDeviceCaps(hdc, 88 /*LOGPIXELSX*/);
                dpiY = GetDeviceCaps(hdc, 90 /*LOGPIXELSY*/);
                DeleteDC(hdc);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal: fall back to 96 DPI.
            Console.WriteLine($"GetMonitorDpi failed: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr CreateDC(string? lpszDriver, string lpszDevice, string? lpszOutput, IntPtr lpInitData);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    public void Dispose() => SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
}
