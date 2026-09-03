namespace NTWallpaper.Infrastructure.Persistence;

using Dapper;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>SQLite-backed implementation of <see cref="ITagRepository"/>.</summary>
public class TagRepository : ITagRepository
{
    private readonly Database _db;

    public TagRepository(Database db) => _db = db;

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        var rows = await c.QueryAsync<Tag>(
            new CommandDefinition("SELECT * FROM Tags ORDER BY \"Order\"", cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<Tag?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        return await c.QueryFirstOrDefaultAsync<Tag>(
            new CommandDefinition("SELECT * FROM Tags WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Tags (Id, Name, IsEnabled, IsGlobalFallback, \"Order\") VALUES (@Id, @Name, @IsEnabled, @IsGlobalFallback, @Order)",
            new { tag.Id, tag.Name, tag.IsEnabled, tag.IsGlobalFallback, tag.Order }, cancellationToken: cancellationToken));
        return tag;
    }

    public async Task UpdateAsync(Tag tag, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE Tags SET Name=@Name, IsEnabled=@IsEnabled, IsGlobalFallback=@IsGlobalFallback, \"Order\"=@Order WHERE Id=@Id",
            new { tag.Id, tag.Name, tag.IsEnabled, tag.IsGlobalFallback, tag.Order }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM GroupTags WHERE TagId = @Id", new { Id = id }, cancellationToken: cancellationToken));
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM Tags WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }
}
