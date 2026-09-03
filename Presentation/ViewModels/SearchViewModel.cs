namespace NTWallpaper.Presentation.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

public partial class SearchViewModel : ObservableObject
{
    private readonly IImageProvider _provider;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _query = "nature mountains";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _hasApiKey;

    public ObservableCollection<ImageResult> Results { get; } = new();

    public SearchViewModel(IImageProvider provider, ISettingsService settings)
    {
        _provider = provider;
        _settings = settings;
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        var key = await _settings.GetApiKeyAsync();
        HasApiKey = !string.IsNullOrEmpty(key);
        StatusMessage = HasApiKey ? "Ready — enter a query and search." : "Set your Pixabay API key in Settings first.";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(Query)) return;
        IsBusy = true;
        Results.Clear();
        StatusMessage = $"Searching for '{Query}'…";
        try
        {
            var options = new ImageSearchOptions { Query = Query, PerPage = 24, SafeSearch = true, Order = "popular" };
            var results = await _provider.SearchAsync(options, CancellationToken.None);
            foreach (var r in results) Results.Add(r);
            StatusMessage = $"Found {results.Count} result(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
