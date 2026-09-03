namespace NTWallpaper.Application.Services;

using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;
using NTWallpaper.Infrastructure.Persistence;

/// <summary>Loads/saves <see cref="AppSettings"/> to the SQLite Settings table and the API key to secure storage.</summary>
public class SettingsService : ISettingsService
{
    private readonly Database _db;
    private readonly ISecureStorage _secure;
    private readonly ILogger<SettingsService> _logger;

    public AppSettings Current { get; private set; } = new();

    public SettingsService(Database db, ISecureStorage secure, ILogger<SettingsService> logger)
    {
        _db = db;
        _secure = secure;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        var rows = await c.QueryAsync<(string Key, string? Value)>(
            new CommandDefinition("SELECT Key, Value FROM Settings", cancellationToken: cancellationToken));
        var dict = rows.ToDictionary(r => r.Key, r => r.Value);

        var settings = new AppSettings();
        foreach (var prop in typeof(AppSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!dict.TryGetValue(prop.Name, out var raw) || raw is null) continue;
            try
            {
                var t = prop.PropertyType;
                if (t == typeof(string)) prop.SetValue(settings, raw);
                else if (t == typeof(int)) prop.SetValue(settings, int.Parse(raw, System.Globalization.CultureInfo.InvariantCulture));
                else if (t == typeof(bool)) prop.SetValue(settings, bool.Parse(raw));
                else if (t.IsEnum) prop.SetValue(settings, Enum.Parse(t, raw));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse setting {Key}", prop.Name);
            }
        }

        if (string.IsNullOrEmpty(settings.CacheDirectory))
            settings.CacheDirectory = DefaultCacheDirectory();
        if (string.IsNullOrEmpty(settings.DatabasePath))
            settings.DatabasePath = DefaultDatabasePath();

        Current = settings;
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        foreach (var prop in typeof(AppSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(Current);
            var str = value?.ToString() ?? string.Empty;
            await c.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Settings (Key, Value) VALUES (@Key, @Value) " +
                "ON CONFLICT(Key) DO UPDATE SET Value = @Value",
                new { Key = prop.Name, Value = str }, cancellationToken: cancellationToken));
        }
    }

    public Task<string?> GetApiKeyAsync() => Task.FromResult(_secure.Load("pixabay_api_key"));

    public Task SetApiKeyAsync(string? apiKey)
    {
        if (apiKey is null) _secure.Delete("pixabay_api_key");
        else _secure.Save("pixabay_api_key", apiKey);
        return Task.CompletedTask;
    }

    public static string DefaultCacheDirectory()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PixabayWallpaper", "Wallpapers");

    public static string DefaultDatabasePath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PixabayWallpaper", "ntwallpaper.db");
}
