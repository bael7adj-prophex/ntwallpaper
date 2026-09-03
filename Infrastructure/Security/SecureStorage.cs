namespace NTWallpaper.Infrastructure.Security;

using System.IO;
using System.Security.Cryptography;
using NTWallpaper.Domain.Interfaces;

/// <summary>
/// Stores secrets (the Pixabay API key) protected with DPAPI (CurrentUser scope).
/// The encrypted blob lives in a file under %LOCALAPPDATA% and never in source, logs, or the DB.
/// </summary>
public class SecureStorage : ISecureStorage
{
    private readonly string _directory;

    public SecureStorage(string directory) => _directory = directory;

    public void Save(string key, string value)
    {
        var path = Path.Combine(_directory, $"{Sanitize(key)}.bin");
        Directory.CreateDirectory(_directory);
        var plain = System.Text.Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, protectedBytes);
    }

    public string? Load(string key)
    {
        var path = Path.Combine(_directory, $"{Sanitize(key)}.bin");
        if (!File.Exists(path)) return null;
        try
        {
            var protectedBytes = File.ReadAllBytes(path);
            var plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(plain);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Delete(string key)
    {
        var path = Path.Combine(_directory, $"{Sanitize(key)}.bin");
        if (File.Exists(path)) File.Delete(path);
    }

    private static string Sanitize(string key) => new string(key.Where(char.IsLetterOrDigit).ToArray());
}