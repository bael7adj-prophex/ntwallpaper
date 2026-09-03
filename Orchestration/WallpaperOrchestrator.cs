namespace NTWallpaper.Orchestration;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Enums;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;
using NTWallpaper.Domain.Options;
using NTWallpaper.Infrastructure.Pixabay;
using NTWallpaper.Infrastructure.Persistence;
using System.IO;

/// <summary>
/// Core engine: for each group it searches Pixabay, recommends the best image,
/// downloads/caches it, renders it, and applies it to the group's targets.
/// Handles tag fallback, cache fallback, per-group serialization, and rescheduling.
/// </summary>
public class WallpaperOrchestrator
{
    private readonly ISettingsService _settings;
    private readonly IGroupRepository _groupsRepo;
    private readonly ITagRepository _tagsRepo;
    private readonly IImageProvider _provider;
    private readonly ICacheService _cache;
    private readonly IRecommendationService _recommendation;
    private readonly IWallpaperService _wallpaper;
    private readonly IMonitorService _monitor;
    private readonly ILockScreenService _lockScreen;
    private readonly IImageRenderingService _rendering;
    private readonly ISchedulerService _scheduler;
    private readonly INotificationService _notification;
    private readonly ILogger<WallpaperOrchestrator> _logger;

    private List<Tag> _tags = new();
    private List<WallpaperTarget> _discoveredTargets = new();
    private Dictionary<string, string> _assignments = new(); // targetId -> groupId
    private readonly HashSet<long> _recentlyUsed = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _groupLocks = new();

    public event EventHandler<StateChangedEventArgs>? StateChanged;

    public event EventHandler<WallpaperAppliedEventArgs>? WallpaperApplied;

    public WallpaperOrchestrator(
        ISettingsService settings,
        IGroupRepository groupsRepo,
        ITagRepository tagsRepo,
        IImageProvider provider,
        ICacheService cache,
        IRecommendationService recommendation,
        IWallpaperService wallpaper,
        IMonitorService monitor,
        ILockScreenService lockScreen,
        IImageRenderingService rendering,
        ISchedulerService scheduler,
        INotificationService notification,
        ILogger<WallpaperOrchestrator> logger)
    {
        _settings = settings;
        _groupsRepo = groupsRepo;
        _tagsRepo = tagsRepo;
        _provider = provider;
        _cache = cache;
        _recommendation = recommendation;
        _wallpaper = wallpaper;
        _monitor = monitor;
        _lockScreen = lockScreen;
        _rendering = rendering;
        _scheduler = scheduler;
        _notification = notification;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _settings.LoadAsync(cancellationToken);
        _tags = (await _tagsRepo.GetAllAsync(cancellationToken)).ToList();
        _discoveredTargets = [.. _monitor.GetTargets()];
        _assignments = await LoadAssignmentsAsync(cancellationToken);

        _monitor.TargetsChanged += OnTargetsChanged;
        _scheduler.GroupDue += OnGroupDue;

        var groups = await _groupsRepo.GetAllAsync(cancellationToken);
        foreach (var g in groups.Where(g => g.IsEnabled))
            ScheduleGroup(g);

        _logger.LogInformation("Orchestrator initialized with {Count} groups.", groups.Count);
    }

    public void Start() => _scheduler.Start();

    public void Stop() => _scheduler.Stop();

    public IReadOnlyList<WallpaperTarget> GetTargets() => _discoveredTargets;

    public IReadOnlyList<Tag> GetTags() => _tags;

    public async Task<IReadOnlyList<WallpaperGroup>> GetGroupsAsync(CancellationToken cancellationToken)
        => await _groupsRepo.GetAllAsync(cancellationToken);

    public DateTime? GetNextRun(string groupId) => _scheduler.GetNextRun(groupId);

    public void TriggerGroup(string groupId)
        => _ = ProcessGroupAsync(groupId, CancellationToken.None);

    // ---- Group / target management ----

    public async Task AddGroupAsync(WallpaperGroup group, CancellationToken cancellationToken)
    {
        await _groupsRepo.AddAsync(group, cancellationToken);
        if (group.IsEnabled) ScheduleGroup(group);
    }

    public async Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken)
    {
        await _groupsRepo.DeleteAsync(groupId, cancellationToken);
        _scheduler.ScheduleGroup(groupId, DateTime.MaxValue); // effectively clears
        _groupLocks.TryRemove(groupId, out _);
    }

    public async Task AssignTargetAsync(string groupId, string targetId, CancellationToken cancellationToken)
    {
        await _groupsRepo.AssignTargetAsync(groupId, targetId, cancellationToken);
        _assignments[targetId] = groupId;
    }

    public async Task UnassignTargetAsync(string targetId, CancellationToken cancellationToken)
    {
        await _groupsRepo.UnassignTargetAsync(targetId, cancellationToken);
        _assignments.Remove(targetId);
    }

    public async Task SetGroupEnabledAsync(string groupId, bool enabled, CancellationToken cancellationToken)
    {
        var group = await _groupsRepo.GetByIdAsync(groupId, cancellationToken);
        if (group is null) return;
        group.IsEnabled = enabled;
        await _groupsRepo.UpdateAsync(group, cancellationToken);
        if (enabled) ScheduleGroup(group);
    }

    // ---- Core pipeline ----

    private async Task ProcessGroupAsync(string groupId, CancellationToken cancellationToken)
    {
        var sem = _groupLocks.GetOrAdd(groupId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(cancellationToken);
        try
        {
            var group = await _groupsRepo.GetByIdAsync(groupId, cancellationToken);
            if (group is null || !group.IsEnabled)
                return;

            var targets = GetTargetsForGroup(group);
            if (targets.Count == 0)
            {
                SetState(group, AppState.Idle, "No targets assigned.");
                return;
            }

            var (primary, fallback) = ResolveTags(group);
            CachedImage? applied = null;

            if (primary.Count == 0 && fallback.Count == 0)
            {
                applied = await UseCacheAsync(group, targets, cancellationToken, "No tags configured; using cached wallpaper.");
            }
            else
            {
                foreach (var tag in Shuffle(primary))
                {
                    applied = await TryTagAsync(group, targets, tag, cancellationToken);
                    if (applied is not null) break;
                }

                if (applied is null)
                {
                    foreach (var tag in Shuffle(fallback))
                    {
                        applied = await TryTagAsync(group, targets, tag, cancellationToken);
                        if (applied is not null) break;
                    }
                }

                if (applied is null)
                    applied = await UseCacheAsync(group, targets, cancellationToken, "Pixabay unavailable; using cached wallpaper.");
            }

            if (applied is not null)
                await ApplyImageAsync(group, targets, applied, cancellationToken);
            else
                SetState(group, AppState.Failed, "No wallpaper available.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing group {GroupId}", groupId);
            _notification.ShowError("Wallpaper error", ex.Message);
            SetState(groupId, AppState.Failed, ex.Message);
        }
        finally
        {
            sem.Release();
            await RescheduleAsync(groupId, cancellationToken);
        }
    }

    private async Task<CachedImage?> TryTagAsync(WallpaperGroup group, IReadOnlyList<WallpaperTarget> targets, string tag, CancellationToken cancellationToken)
    {
        try
        {
            SetState(group, AppState.Searching, $"Searching '{tag}'...");
            var options = BuildOptions(tag, group, targets);
            var results = await _provider.SearchAsync(options, cancellationToken);
            if (results.Count == 0) return null;

            var best = _recommendation.SelectBest(results, targets, _recentlyUsed, _settings.Current);
            if (best is null) return null;

            SetState(group, AppState.Downloading, $"Downloading '{tag}'...");
            var cached = await _cache.GetOrDownloadAsync(best, cancellationToken);
            if (cached is null) return null;

            _recentlyUsed.Add(best.PixabayId);
            if (_recentlyUsed.Count > 50) _recentlyUsed.Remove(_recentlyUsed.First());
            return cached;
        }
        catch (PixabayException ex) when (!ex.IsRetryable)
        {
            _logger.LogWarning(ex, "Pixabay rejected search for '{Tag}'", tag);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search failed for '{Tag}'", tag);
            return null;
        }
    }

    private async Task<CachedImage?> UseCacheAsync(WallpaperGroup group, IReadOnlyList<WallpaperTarget> targets, CancellationToken cancellationToken, string reason)
    {
        SetState(group, AppState.UsingCache, reason);
        var all = await _cache.GetAllAsync(cancellationToken);
        return all.Where(i => File.Exists(i.LocalPath)).OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
    }

    private async Task ApplyImageAsync(WallpaperGroup group, IReadOnlyList<WallpaperTarget> targets, CachedImage image, CancellationToken cancellationToken)
    {
        SetState(group, AppState.Applying);
        try
        {
            var monitorTargets = targets.Where(t => t.Kind == TargetKind.Monitor).ToList();
            var lockTargets = targets.Where(t => t.Kind == TargetKind.LockScreen).ToList();

            if (monitorTargets.Count > 0)
            {
                var renderPath = await _rendering.RenderAsync(image.LocalPath, monitorTargets, group.WallpaperStyle, cancellationToken);
                await _wallpaper.ApplyWallpaperAsync(renderPath, group.WallpaperStyle, monitorTargets.Select(t => t.Id), cancellationToken);
            }

            if (lockTargets.Count > 0)
            {
                if (_lockScreen.IsSupported)
                {
                    var result = await _lockScreen.SetLockScreenAsync(image.LocalPath, cancellationToken);
                    if (!result.Success)
                        _notification.ShowWarning("Lock screen", result.Message ?? "Failed to set lock screen.");
                }
                else
                {
                    _logger.LogInformation("Lock screen not supported; skipping for group {Group}", group.Name);
                }
            }

            await _cache.MarkUsedAsync(image.Id, cancellationToken);
            SetState(group, AppState.Applied, $"Applied {Path.GetFileName(image.LocalPath)}");
            WallpaperApplied?.Invoke(this, new WallpaperAppliedEventArgs { GroupId = group.Id, ImagePath = image.LocalPath, Success = true });
            _notification.ShowInfo("New wallpaper", $"Applied to {group.Name}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply wallpaper for group {Group}", group.Name);
            SetState(group, AppState.Failed, ex.Message);
            _notification.ShowError("Apply failed", ex.Message);
        }
    }

    // ---- Helpers ----

    private List<WallpaperTarget> GetTargetsForGroup(WallpaperGroup group)
        => _discoveredTargets.Where(t => _assignments.TryGetValue(t.Id, out var gid) && gid == group.Id).ToList();

    private (List<string> Primary, List<string> Fallback) ResolveTags(WallpaperGroup group)
    {
        var enabled = _tags.Where(t => t.IsEnabled).ToDictionary(t => t.Id);

        List<string> Primary()
        {
            var list = group.TagIds.Where(enabled.ContainsKey).Select(id => enabled[id].Name).ToList();
            if (list.Count == 0 && _settings.Current.UseGlobalFallbackWhenGroupEmpty)
                list = _tags.Where(t => t.IsEnabled && t.IsGlobalFallback).Select(t => t.Name).ToList();
            return list;
        }

        var primary = Primary();
        var fallback = group.FallbackTagIds.Where(enabled.ContainsKey).Select(id => enabled[id].Name).ToList();
        foreach (var g in _tags.Where(t => t.IsEnabled && t.IsGlobalFallback).Select(t => t.Name))
            if (!fallback.Contains(g)) fallback.Add(g);

        return (primary, fallback);
    }

    private ImageSearchOptions BuildOptions(string tag, WallpaperGroup group, IReadOnlyList<WallpaperTarget> targets)
    {
        var s = _settings.Current;
        var avgAspect = targets.Average(t => t.AspectRatio);
        var orientation = s.Orientation;
        if (orientation == Orientation.All)
            orientation = avgAspect >= 1.0 ? Orientation.Horizontal : Orientation.Vertical;

        var minW = s.MinWidth > 0 ? s.MinWidth : targets.Max(t => t.Width);
        var minH = s.MinHeight > 0 ? s.MinHeight : targets.Max(t => t.Height);

        return new ImageSearchOptions
        {
            Query = tag,
            Lang = s.Language,
            Category = s.Category,
            ImageType = s.ImageType,
            Orientation = orientation,
            MinWidth = minW,
            MinHeight = minH,
            Colors = s.Colors,
            EditorsChoice = s.EditorsChoice,
            SafeSearch = s.SafeSearch,
            Order = s.Order,
            Page = 1,
            PerPage = s.PerPage
        };
    }

    private void ScheduleGroup(WallpaperGroup group)
    {
        var next = ComputeNextRun(group, DateTime.UtcNow);
        if (next is null) return;
        group.NextRotationUtc = next;
        _ = _groupsRepo.UpdateAsync(group, CancellationToken.None);
        _scheduler.ScheduleGroup(group.Id, next.Value);
    }

    private async Task RescheduleAsync(string groupId, CancellationToken cancellationToken)
    {
        var group = await _groupsRepo.GetByIdAsync(groupId, cancellationToken);
        if (group is null || !group.IsEnabled) return;
        ScheduleGroup(group);
    }

    public static DateTime? ComputeNextRun(WallpaperGroup group, DateTime now)
    {
        return group.RotationInterval switch
        {
            RotationInterval.Disabled => null,
            RotationInterval.Minutes15 => now.AddMinutes(15),
            RotationInterval.Minutes30 => now.AddMinutes(30),
            RotationInterval.Hour1 => now.AddHours(1),
            RotationInterval.Hours2 => now.AddHours(2),
            RotationInterval.Hours4 => now.AddHours(4),
            RotationInterval.Hours6 => now.AddHours(6),
            RotationInterval.Hours12 => now.AddHours(12),
            RotationInterval.Daily => now.AddDays(1),
            RotationInterval.Custom => group.CustomInterval.HasValue ? now.Add(group.CustomInterval.Value) : null,
            RotationInterval.SpecificTime when group.SpecificTime.HasValue =>
                now.TimeOfDay < group.SpecificTime.Value.ToTimeSpan()
                    ? now.Date.Add(group.SpecificTime.Value.ToTimeSpan())
                    : now.Date.AddDays(1).Add(group.SpecificTime.Value.ToTimeSpan()),
            _ => null
        };
    }

    private async Task<Dictionary<string, string>> LoadAssignmentsAsync(CancellationToken cancellationToken)
        => await _groupsRepo.GetAssignmentsAsync(cancellationToken);

    private void OnTargetsChanged(object? sender, TargetsChangedEventArgs e)
    {
        _discoveredTargets = e.Targets.ToList();
        // Drop assignments whose target no longer exists.
        foreach (var targetId in _assignments.Keys.ToList())
        {
            if (!_discoveredTargets.Any(t => t.Id == targetId))
            {
                _assignments.Remove(targetId);
                _ = _groupsRepo.UnassignTargetAsync(targetId, CancellationToken.None);
            }
        }

        _logger.LogInformation("Targets refreshed: {Count} available.", _discoveredTargets.Count);
    }

    private void OnGroupDue(object? sender, GroupDueEventArgs e)
        => _ = ProcessGroupAsync(e.GroupId, CancellationToken.None);

    private static List<string> Shuffle(List<string> input)
    {
        var list = input.ToList();
        var rng = new Random();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private void SetState(WallpaperGroup group, AppState state, string? detail = null)
    {
        group.State = state;
        StateChanged?.Invoke(this, new StateChangedEventArgs { GroupId = group.Id, State = state, Detail = detail });
    }

    private void SetState(string groupId, AppState state, string? detail = null)
        => StateChanged?.Invoke(this, new StateChangedEventArgs { GroupId = groupId, State = state, Detail = detail });
}