namespace NTWallpaper.Infrastructure.Windows;

using Microsoft.Win32;
using NTWallpaper.Domain.Interfaces;

/// <summary>Registers the app to start with Windows via the HKCU Run key (no admin required).</summary>
public class StartupService : IStartupService
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NTWallpaper";

    private static string ExePath
    {
        get
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            return process.MainModule?.FileName ?? string.Empty;
        }
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        var value = key?.GetValue(ValueName) as string;
        return !string.IsNullOrEmpty(value) && File.Exists(value);
    }

    public void Enable()
    {
        var exe = ExePath;
        if (string.IsNullOrEmpty(exe)) return;
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        key.SetValue(ValueName, exe);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
