namespace NTWallpaper.Domain.Interfaces;

using NTWallpaper.Domain.Models;

/// <summary>Persistent store for downloaded images / history.</summary>
public interface IImageRepository
{
    Task<CachedImage?> GetByPixabayIdAsync(long pixabayId, CancellationToken cancellationToken);
    Task<CachedImage?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<long> UpsertAsync(CachedImage image, CancellationToken cancellationToken);
    Task<IReadOnlyList<CachedImage>> GetAllAsync(CancellationToken cancellationToken);
    Task MarkUsedAsync(long id, CancellationToken cancellationToken);
    Task SetFavoriteAsync(long id, bool favorite, CancellationToken cancellationToken);
    Task SetRatingAsync(long id, int rating, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
}

/// <summary>Persistent store for wallpaper groups and target assignments.</summary>
public interface IGroupRepository
{
    Task<IReadOnlyList<WallpaperGroup>> GetAllAsync(CancellationToken cancellationToken);
    Task<WallpaperGroup?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<string> AddAsync(WallpaperGroup group, CancellationToken cancellationToken);
    Task UpdateAsync(WallpaperGroup group, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
    Task AssignTargetAsync(string groupId, string targetId, CancellationToken cancellationToken);
    Task UnassignTargetAsync(string targetId, CancellationToken cancellationToken);
    Task<Dictionary<string, string>> GetAssignmentsAsync(CancellationToken cancellationToken);
}

/// <summary>Persistent store for interest tags.</summary>
public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken);
    Task<Tag?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken);
    Task UpdateAsync(Tag tag, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}

/// <summary>Application settings persistence (API key handled separately via secure storage).</summary>
public interface ISettingsService
{
    AppSettings Current { get; }

    Task LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<string?> GetApiKeyAsync();
    Task SetApiKeyAsync(string? apiKey);
}

/// <summary>Protects secrets (API key) using DPAPI or the Windows Credential Manager.</summary>
public interface ISecureStorage
{
    void Save(string key, string value);
    string? Load(string key);
    void Delete(string key);
}
