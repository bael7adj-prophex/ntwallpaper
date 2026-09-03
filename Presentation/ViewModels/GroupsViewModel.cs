namespace NTWallpaper.Presentation.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTWallpaper.Application.Orchestration;
using NTWallpaper.Domain.Enums;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

public partial class GroupsViewModel : ObservableObject
{
    private readonly WallpaperOrchestrator _orchestrator;
    private readonly IGroupRepository _groupsRepo;
    private readonly ITagRepository _tagsRepo;

    [ObservableProperty] private string _newGroupName = "Main Displays";
    [ObservableProperty] private RotationInterval _newGroupInterval = RotationInterval.Hour1;

    public ObservableCollection<WallpaperGroup> Groups { get; } = new();
    public ObservableCollection<WallpaperTarget> Targets { get; } = new();
    public ObservableCollection<Tag> Tags { get; } = new();
    public Array RotationIntervals => Enum.GetValues<RotationInterval>();

    public GroupsViewModel(WallpaperOrchestrator orchestrator, IGroupRepository groupsRepo, ITagRepository tagsRepo, IMonitorService monitor)
    {
        _orchestrator = orchestrator;
        _groupsRepo = groupsRepo;
        _tagsRepo = tagsRepo;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Groups.Clear();
        foreach (var g in await _groupsRepo.GetAllAsync(CancellationToken.None))
            Groups.Add(g);

        Targets.Clear();
        foreach (var t in _orchestrator.GetTargets())
            Targets.Add(t);

        Tags.Clear();
        foreach (var t in await _tagsRepo.GetAllAsync(CancellationToken.None))
            Tags.Add(t);
    }

    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName)) return;
        var group = new WallpaperGroup { Name = NewGroupName, RotationInterval = NewGroupInterval };
        await _orchestrator.AddGroupAsync(group, CancellationToken.None);
        NewGroupName = string.Empty;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(WallpaperGroup? group)
    {
        if (group is null) return;
        await _orchestrator.DeleteGroupAsync(group.Id, CancellationToken.None);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AssignTargetAsync((WallpaperGroup? group, WallpaperTarget? target) pair)
    {
        if (pair.group is null || pair.target is null) return;
        await _orchestrator.AssignTargetAsync(pair.group.Id, pair.target.Id, CancellationToken.None);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task UnassignTargetAsync(WallpaperTarget? target)
    {
        if (target is null) return;
        await _orchestrator.UnassignTargetAsync(target.Id, CancellationToken.None);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync(WallpaperGroup? group)
    {
        if (group is null) return;
        group.IsEnabled = !group.IsEnabled;
        await _orchestrator.SetGroupEnabledAsync(group.Id, group.IsEnabled, CancellationToken.None);
        await RefreshAsync();
    }
}
