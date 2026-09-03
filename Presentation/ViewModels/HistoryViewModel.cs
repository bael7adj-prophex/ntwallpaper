namespace NTWallpaper.Presentation.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ICacheService _cache;
    private readonly IImageRepository _repo;

    public ObservableCollection<CachedImage> Images { get; } = new();

    [ObservableProperty] private CachedImage? _selected;

    public HistoryViewModel(ICacheService cache, IImageRepository repo)
    {
        _cache = cache;
        _repo = repo;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Images.Clear();
        foreach (var img in await _cache.GetAllAsync(CancellationToken.None))
            Images.Add(img);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(CachedImage? image)
    {
        if (image is null) return;
        image.IsFavorite = !image.IsFavorite;
        await _repo.SetFavoriteAsync(image.Id, image.IsFavorite, CancellationToken.None);
    }

    [RelayCommand]
    private async Task RateAsync(CachedImage? image)
    {
        if (image is null || Selected is null) return;
        image.Rating = image.Rating >= 5 ? 0 : image.Rating + 1;
        await _repo.SetRatingAsync(image.Id, image.Rating, CancellationToken.None);
    }

    [RelayCommand]
    private async Task DeleteAsync(CachedImage? image)
    {
        if (image is null) return;
        await _cache.DeleteAsync(image.Id, CancellationToken.None);
        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenFolder(CachedImage? image)
    {
        if (image is null || !File.Exists(image.LocalPath)) return;
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{image.LocalPath}\"");
    }
}
