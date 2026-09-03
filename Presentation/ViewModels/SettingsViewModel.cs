namespace NTWallpaper.Presentation.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTWallpaper.Domain.Enums;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IStartupService _startup;

    [ObservableProperty] private string? _apiKey;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private ThemePreference _theme;
    [ObservableProperty] private int _retryCount;
    [ObservableProperty] private string _statusMessage = "Settings saved automatically on change.";

    public Array ThemeOptions => Enum.GetValues<ThemePreference>();

    public SettingsViewModel(ISettingsService settings, IStartupService startup)
    {
        _settings = settings;
        _startup = startup;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var current = _settings.Current;
        ApiKey = await _settings.GetApiKeyAsync();
        StartWithWindows = _startup.IsEnabled();
        StartMinimized = current.StartMinimized;
        Theme = current.Theme;
        RetryCount = current.RetryCount;
    }

    partial void OnApiKeyChanged(string? value) => _ = SaveApiKeyAsync(value);
    partial void OnStartWithWindowsChanged(bool value) { if (value) _startup.Enable(); else _startup.Disable(); _ = SaveGeneralAsync(); }
    partial void OnStartMinimizedChanged(bool value) => _ = SaveGeneralAsync();
    partial void OnThemeChanged(ThemePreference value) => _ = SaveGeneralAsync();
    partial void OnRetryCountChanged(int value) => _ = SaveGeneralAsync();

    private async Task SaveApiKeyAsync(string? value)
    {
        await _settings.SetApiKeyAsync(value);
        StatusMessage = "API key saved (DPAPI-encrypted).";
    }

    private async Task SaveGeneralAsync()
    {
        var s = _settings.Current;
        s.StartMinimized = StartMinimized;
        s.Theme = Theme;
        s.RetryCount = RetryCount;
        await _settings.SaveAsync(CancellationToken.None);
        StatusMessage = "Settings saved.";
    }
}
