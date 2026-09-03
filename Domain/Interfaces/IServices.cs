namespace NTWallpaper.Domain.Interfaces;

using NTWallpaper.Domain.Enums;
using NTWallpaper.Domain.Models;

/// <summary>Selects the best candidate image for a group's targets.</summary>
public interface IRecommendationService
{
    ImageResult? SelectBest(IReadOnlyList<ImageResult> candidates, IReadOnlyList<WallpaperTarget> targets, ISet<long> recentlyUsed, AppSettings settings);
}

/// <summary>Background scheduler that fires <see cref="GroupDueEventArgs"/> when a group is due.</summary>
public interface ISchedulerService
{
    void Start();
    void Stop();
    void ScheduleGroup(string groupId, DateTime nextRunUtc);
    void TriggerNow(string groupId);
    DateTime? GetNextRun(string groupId);
    event EventHandler<GroupDueEventArgs>? GroupDue;
}

/// <summary>Local image cache: downloads on demand, prevents duplicate downloads, tracks usage.</summary>
public interface ICacheService
{
    Task<CachedImage?> GetOrDownloadAsync(ImageResult result, CancellationToken cancellationToken);
    Task<CachedImage?> GetCachedAsync(long pixabayId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CachedImage>> GetAllAsync(CancellationToken cancellationToken);
    Task MarkUsedAsync(long id, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
    Task<long> GetCacheSizeBytesAsync(CancellationToken cancellationToken);
}

/// <summary>User-facing notifications (toast / tray balloon).</summary>
public interface INotificationService
{
    void ShowInfo(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
}

/// <summary>Windows startup registration (no administrator required).</summary>
public interface IStartupService
{
    bool IsEnabled();
    void Enable();
    void Disable();
}
