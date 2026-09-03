namespace NTWallpaper.Infrastructure.Persistence;

using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>SQLite-backed implementation of <see cref="IImageRepository"/>.</summary>
public class ImageRepository : IImageRepository
{
    private readonly Database _db;

    public ImageRepository(Database db) => _db = db;

    public async Task<CachedImage?> GetByPixabayIdAsync(long pixabayId, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        return await c.QueryFirstOrDefaultAsync<CachedImage>(
            new CommandDefinition("SELECT * FROM Images WHERE PixabayId = @Id", new { Id = pixabayId }, cancellationToken: cancellationToken));
    }

    public async Task<CachedImage?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        return await c.QueryFirstOrDefaultAsync<CachedImage>(
            new CommandDefinition("SELECT * FROM Images WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<long> UpsertAsync(CachedImage image, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        var existing = await c.QueryFirstOrDefaultAsync<long?>(
            new CommandDefinition("SELECT Id FROM Images WHERE PixabayId = @PixabayId", new { image.PixabayId }, cancellationToken: cancellationToken));

        if (existing is null)
        {
            return await c.ExecuteScalarAsync<long>(new CommandDefinition(
                @"INSERT INTO Images (PixabayId, SourceUrl, PreviewUrl, LargeImageUrl, LocalPath, SearchTerm, Width, Height, ImageType, Orientation, Colors, DownloadedAtUtc, TimesDisplayed, IsFavorite, Rating)
                  VALUES (@PixabayId, @SourceUrl, @PreviewUrl, @LargeImageUrl, @LocalPath, @SearchTerm, @Width, @Height, @ImageType, @Orientation, @Colors, @DownloadedAtUtc, 0, 0, 0);
                  SELECT last_insert_rowid();",
                image, cancellationToken: cancellationToken));
        }

        await c.ExecuteAsync(new CommandDefinition(
            @"UPDATE Images SET SourceUrl=@SourceUrl, PreviewUrl=@PreviewUrl, LargeImageUrl=@LargeImageUrl, LocalPath=@LocalPath, SearchTerm=@SearchTerm, Width=@Width, Height=@Height, ImageType=@ImageType, Orientation=@Orientation, Colors=@Colors WHERE PixabayId=@PixabayId",
            image, cancellationToken: cancellationToken));
        return existing.Value;
    }

    public async Task<IReadOnlyList<CachedImage>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        var rows = await c.QueryAsync<CachedImage>(
            new CommandDefinition("SELECT * FROM Images ORDER BY DownloadedAtUtc DESC", cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task MarkUsedAsync(long id, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE Images SET TimesDisplayed = TimesDisplayed + 1, FirstUsedAtUtc = COALESCE(FirstUsedAtUtc, @Now), LastUsedAtUtc = @Now WHERE Id = @Id",
            new { Id = id, Now = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }

    public async Task SetFavoriteAsync(long id, bool favorite, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition("UPDATE Images SET IsFavorite = @Fav WHERE Id = @Id", new { Id = id, Fav = favorite }, cancellationToken: cancellationToken));
    }

    public async Task SetRatingAsync(long id, int rating, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition("UPDATE Images SET Rating = @Rating WHERE Id = @Id", new { Id = id, Rating = rating }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM Images WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        await using var c = _db.Open();
        return await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM Images", cancellationToken: cancellationToken));
    }
}
