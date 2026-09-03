namespace NTWallpaper.Presentation.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTWallpaper.Application.Orchestration;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

public partial class DashboardViewModel : ObservableObject
{
    private readonly WallpaperOrchestrator _orchestrator;
    private readonly ICacheService _cache;

    [ObservableProperty] private string _currentWallpaperPath = string.Empty;
    [ObservableProperty] private long _cacheSizeBytes;
    [ObservableProperty] private int _imageCount;
    [ObservableProperty] private string _statusMessage = "Ready";

    public ObservableCollection<GroupSummary> Groups { get; } = new();

    public DashboardViewModel(WallpaperOrchestrator orchestrator, ICacheService cache, IMonitorService monitor)
    {
        _orchestrator = orchestrator;
        _cache = cache;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var groups = await _orchestrator.GetGroupsAsync(CancellationToken.None);
        var monitorTargets = _orchestrator.GetTargets();
        Groups.Clear();
        foreach (var g in groups)
        {
            var targets = monitorTargets.Count(t => t.GroupId == g.Id);
            Groups.Add(new GroupSummary(g, targets, _orchestrator.GetNextRun(g.Id)));
        }

        CacheSizeBytes = await _cache.GetCacheSizeBytesAsync(CancellationToken.None);
        var all = await _cache.GetAllAsync(CancellationToken.None);
        ImageCount = all.Count;
        StatusMessage = $"Active — {groups.Count} group(s), {ImageCount} image(s) cached";
    }

    [RelayCommand]
    private void NewWallpaper(string? groupId)
    {
        if (string.IsNullOrEmpty(groupId)) return;
        _orchestrator.TriggerGroup(groupId);
        StatusMessage = $"Triggered '{groupId}'";
    }

    [RelayCommand]
    private void PauseAll()
    {
        foreach (var g in Groups) g.IsEnabled = false;
        StatusMessage = "All groups paused";
    }
}

public class GroupSummary
{
    public string Id { get; }
    public string Name { get; }
    public bool IsEnabled { get; set; }
    public int TargetCount { get; }
    public DateTime? NextRun { get; }
    public string NextRunDisplay => NextRun is null ? "—" : NextRun.Value.ToLocalTime().ToString("g");
    public WallpaperGroup Group { get; }

    public GroupSummary(WallpaperGroup group, int targetCount, DateTime? nextRun)
    {
        Group = group;
        Id = group.Id;
        Name = group.Name;
        IsEnabled = group.IsEnabled;
        TargetCount = targetCount;
        NextRun = nextRun;
    }
}
