namespace NTWallpaper.Infrastructure.Windows;

using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Interfaces;
using System.IO;

/// <summary>
/// Lock-screen integration via the Windows Personalization CSP registry.
/// Windows does NOT expose a per-user, non-admin API for the lock screen image,
/// so this is only possible with administrator privileges. We never report success
/// unless the operation actually succeeded.
/// </summary>
public class LockScreenService : ILockScreenService
{
    private const string CspSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";

    private readonly ILogger<LockScreenService> _logger;

    public LockScreenService(ILogger<LockScreenService> logger) => _logger = logger;

    public bool IsSupported => IsAdministrator();

    public async Task<LockScreenResult> SetLockScreenAsync(string imagePath, CancellationToken cancellationToken)
    {
        if (!IsSupported)
            return new LockScreenResult
            {
                Success = false,
                Message = "Windows does not allow setting the lock screen without administrator privileges (Personalization CSP). Feature disabled."
            };

        try
        {
            var destDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "SystemData", "NTWallpaperLockScreen");
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, "lockscreen.jpg");
            File.Copy(imagePath, dest, overwrite: true);

            using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(CspSubKey);
            key.SetValue("LockScreenImagePath", dest);
            key.SetValue("LockScreenImageStatus", 1, Microsoft.Win32.RegistryValueKind.DWord);
            key.SetValue("LockScreenImageUrl", dest);

            _logger.LogInformation("Lock screen image set via Personalization CSP.");
            return new LockScreenResult
            {
                Success = true,
                Message = "Lock screen updated. It applies through the Windows Personalization CSP and may require a sign-out to take effect."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set lock screen.");
            return new LockScreenResult { Success = false, Message = $"Failed to set lock screen: {ex.Message}" };
        }
    }

    private static bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}