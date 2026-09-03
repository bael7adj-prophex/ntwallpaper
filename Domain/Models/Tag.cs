namespace NTWallpaper.Domain.Models;

/// <summary>A search interest/tag. Tags can be enabled/disabled and promoted to global fallback.</summary>
public class Tag
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsGlobalFallback { get; set; }
    public int Order { get; set; }
}
