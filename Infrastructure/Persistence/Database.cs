namespace NTWallpaper.Infrastructure.Persistence;

using System.Data;
using Microsoft.Data.Sqlite;

/// <summary>Owns the SQLite connection and schema migrations.</summary>
public class Database
{
    private readonly string _connectionString;

    public Database(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        _connectionString = $"Data Source={databasePath}";
        SqliteTypeHandlers.EnsureRegistered();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Initialize()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        ApplyMigrations(connection, transaction);
        transaction.Commit();
    }

    private static void ApplyMigrations(SqliteConnection connection, IDbTransaction transaction)
    {
        var scripts = new[]
        {
            @"
                CREATE TABLE IF NOT EXISTS Images (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PixabayId INTEGER NOT NULL UNIQUE,
                    SourceUrl TEXT NOT NULL,
                    PreviewUrl TEXT,
                    LargeImageUrl TEXT,
                    LocalPath TEXT NOT NULL,
                    SearchTerm TEXT,
                    Width INTEGER,
                    Height INTEGER,
                    ImageType TEXT,
                    Orientation TEXT,
                    Colors TEXT,
                    DownloadedAtUtc INTEGER,
                    FirstUsedAtUtc INTEGER,
                    LastUsedAtUtc INTEGER,
                    TimesDisplayed INTEGER,
                    IsFavorite INTEGER,
                    Rating INTEGER
                );
                CREATE TABLE IF NOT EXISTS Tags (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    IsEnabled INTEGER,
                    IsGlobalFallback INTEGER,
                    ""Order"" INTEGER
                );
                CREATE TABLE IF NOT EXISTS Groups (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    IsEnabled INTEGER,
                    RotationInterval INTEGER,
                    CustomIntervalTicks INTEGER,
                    SpecificTimeTicks INTEGER,
                    WallpaperStyle INTEGER,
                    NextRotationUtc INTEGER,
                    State INTEGER
                );
                CREATE TABLE IF NOT EXISTS GroupTags (
                    GroupId TEXT NOT NULL,
                    TagId TEXT NOT NULL,
                    IsFallback INTEGER,
                    PRIMARY KEY (GroupId, TagId, IsFallback)
                );
                CREATE TABLE IF NOT EXISTS Targets (
                    TargetId TEXT PRIMARY KEY,
                    GroupId TEXT
                );
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );
                CREATE TABLE IF NOT EXISTS WallpaperApplications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GroupId TEXT,
                    ImageId INTEGER,
                    AppliedAtUtc INTEGER,
                    Success INTEGER
                );
                CREATE TABLE IF NOT EXISTS SchemaVersion (
                    Version INTEGER PRIMARY KEY,
                    AppliedAtUtc INTEGER
                );
            "
        };

        var current = connection.ExecuteScalar<int?>("SELECT MAX(Version) FROM SchemaVersion;", transaction: transaction) ?? 0;
        for (var i = current; i < scripts.Length; i++)
        {
            connection.Execute(scripts[i], transaction: transaction);
            connection.Execute("INSERT INTO SchemaVersion (Version, AppliedAtUtc) VALUES (@Version, @Now);",
                new { Version = i + 1, Now = DateTime.UtcNow.Ticks }, transaction: transaction);
        }
    }
}
