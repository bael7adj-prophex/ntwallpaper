namespace NTWallpaper.Infrastructure.Persistence;

using Dapper;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>SQLite-backed implementation of <see cref="IGroupRepository"/>.</summary>
public class GroupRepository : IGroupRepository
{
    private readonly Database _db;

    public GroupRepository(Database db) => _db = db;

    public async Task<IReadOnlyList<WallpaperGroup>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        var groups = (await c.QueryAsync<WallpaperGroup>(
            new CommandDefinition("SELECT * FROM Groups", cancellationToken: cancellationToken))).ToList();
        foreach (var g in groups)
            await LoadTagsAsync(c, g, cancellationToken);
        return groups;
    }

    public async Task<WallpaperGroup?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        var g = await c.QueryFirstOrDefaultAsync<WallpaperGroup>(
            new CommandDefinition("SELECT * FROM Groups WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
        if (g is not null)
            await LoadTagsAsync(c, g, cancellationToken);
        return g;
    }

    public async Task<string> AddAsync(WallpaperGroup group, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO Groups (Id, Name, IsEnabled, RotationInterval, CustomIntervalTicks, SpecificTimeTicks, WallpaperStyle, NextRotationUtc, State)
              VALUES (@Id, @Name, @IsEnabled, @RotationInterval, @CustomIntervalTicks, @SpecificTimeTicks, @WallpaperStyle, @NextRotationUtc, @State)",
            new
            {
                group.Id,
                group.Name,
                group.IsEnabled,
                RotationInterval = (int)group.RotationInterval,
                CustomIntervalTicks = group.CustomInterval?.Ticks,
                SpecificTimeTicks = group.SpecificTime?.Ticks,
                WallpaperStyle = (int)group.WallpaperStyle,
                group.NextRotationUtc,
                State = (int)group.State
            }, cancellationToken: cancellationToken));
        await SaveTagsAsync(c, group, cancellationToken);
        return group.Id;
    }

    public async Task UpdateAsync(WallpaperGroup group, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition(
            @"UPDATE Groups SET Name=@Name, IsEnabled=@IsEnabled, RotationInterval=@RotationInterval, CustomIntervalTicks=@CustomIntervalTicks, SpecificTimeTicks=@SpecificTimeTicks, WallpaperStyle=@WallpaperStyle, NextRotationUtc=@NextRotationUtc, State=@State WHERE Id=@Id",
            new
            {
                group.Id,
                group.Name,
                group.IsEnabled,
                RotationInterval = (int)group.RotationInterval,
                CustomIntervalTicks = group.CustomInterval?.Ticks,
                SpecificTimeTicks = group.SpecificTime?.Ticks,
                WallpaperStyle = (int)group.WallpaperStyle,
                group.NextRotationUtc,
                State = (int)group.State
            }, cancellationToken: cancellationToken));
        await SaveTagsAsync(c, group, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM GroupTags WHERE GroupId = @Id", new { Id = id }, cancellationToken: cancellationToken));
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM Targets WHERE GroupId = @Id", new { Id = id }, cancellationToken: cancellationToken));
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM Groups WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task AssignTargetAsync(string groupId, string targetId, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM Targets WHERE TargetId = @TargetId", new { TargetId = targetId }, cancellationToken: cancellationToken));
        await c.ExecuteAsync(new CommandDefinition("INSERT INTO Targets (TargetId, GroupId) VALUES (@TargetId, @GroupId)", new { TargetId = targetId, GroupId = groupId }, cancellationToken: cancellationToken));
    }

    public async Task UnassignTargetAsync(string targetId, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM Targets WHERE TargetId = @TargetId", new { TargetId = targetId }, cancellationToken: cancellationToken));
    }

    public async Task<Dictionary<string, string>> GetAssignmentsAsync(CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        var rows = await c.QueryAsync<(string TargetId, string GroupId)>(
            new CommandDefinition("SELECT TargetId, GroupId FROM Targets WHERE GroupId IS NOT NULL", cancellationToken: cancellationToken));
        return rows.ToDictionary(r => r.TargetId, r => r.GroupId);
    }

    private static async Task LoadTagsAsync(Microsoft.Data.Sqlite.SqliteConnection c, WallpaperGroup g, CancellationToken ct)
    {
        var rows = await c.QueryAsync<(string TagId, int IsFallback)>(
            new CommandDefinition("SELECT TagId, IsFallback FROM GroupTags WHERE GroupId = @Id", new { Id = g.Id }, cancellationToken: ct));
        foreach (var r in rows)
        {
            if (r.IsFallback == 1) g.FallbackTagIds.Add(r.TagId);
            else g.TagIds.Add(r.TagId);
        }
    }

    private static async Task SaveTagsAsync(Microsoft.Data.Sqlite.SqliteConnection c, WallpaperGroup g, CancellationToken ct)
    {
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM GroupTags WHERE GroupId = @Id", new { Id = g.Id }, cancellationToken: ct));
        foreach (var tagId in g.TagIds)
            await c.ExecuteAsync(new CommandDefinition("INSERT OR IGNORE INTO GroupTags (GroupId, TagId, IsFallback) VALUES (@GroupId, @TagId, 0)", new { GroupId = g.Id, TagId = tagId }, cancellationToken: ct));
        foreach (var tagId in g.FallbackTagIds)
            await c.ExecuteAsync(new CommandDefinition("INSERT OR IGNORE INTO GroupTags (GroupId, TagId, IsFallback) VALUES (@GroupId, @TagId, 1)", new { GroupId = g.Id, TagId = tagId }, cancellationToken: ct));
    }
}
