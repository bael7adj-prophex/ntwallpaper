namespace NTWallpaper.Infrastructure.Pixabay;

/// <summary>Strongly-typed Pixabay API response (subset of fields we use).</summary>
public class PixabayResponse
{
    public int Total { get; set; }
    public int TotalHits { get; set; }
    public List<PixabayHit> Hits { get; set; } = new();
}

public class PixabayHit
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string PageURL { get; set; } = string.Empty;
    public string PreviewURL { get; set; } = string.Empty;
    public int PreviewWidth { get; set; }
    public int PreviewHeight { get; set; }
    public string WebformatURL { get; set; } = string.Empty;
    public int WebformatWidth { get; set; }
    public int WebformatHeight { get; set; }
    public string LargeImageURL { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public int ImageSize { get; set; }
    public int Views { get; set; }
    public int Downloads { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
    public int Favorites { get; set; }
    public int UserId { get; set; }
    public string User { get; set; } = string.Empty;
    public string UserImageURL { get; set; } = string.Empty;
}

/// <summary>Raised for Pixabay API / transport failures. <see cref="IsRetryable"/> guides backoff.</summary>
public class PixabayException : Exception
{
    public bool IsRetryable { get; }

    public PixabayException(string message, bool retryable = false, Exception? inner = null)
        : base(message, inner)
    {
        IsRetryable = retryable;
    }
}
