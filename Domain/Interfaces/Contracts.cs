namespace NTWallpaper.Domain.Interfaces;

/// <summary>Result of an attempt to set the Windows lock screen wallpaper.</summary>
public class LockScreenResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
}

public class TargetsChangedEventArgs : EventArgs
{
    public IReadOnlyList<NTWallpaper.Domain.Models.WallpaperTarget> Targets { get; init; } = Array.Empty<NTWallpaper.Domain.Models.WallpaperTarget>();
}

public class GroupDueEventArgs : EventArgs
{
    public string GroupId { get; init; } = string.Empty;
}

public class WallpaperAppliedEventArgs : EventArgs
{
    public string GroupId { get; init; } = string.Empty;
    public string? ImagePath { get; init; }
    public bool Success { get; init; }
    public string? Message { get; init; }
}

public class StateChangedEventArgs : EventArgs
{
    public string GroupId { get; init; } = string.Empty;
    public NTWallpaper.Domain.Enums.AppState State { get; init; }
    public string? Detail { get; init; }
}

public enum NotificationKind
{
    Info,
    Warning,
    Error
}

public class NotificationEventArgs : EventArgs
{
    public string Title { get; }
    public string Message { get; }
    public NotificationKind Kind { get; }

    public NotificationEventArgs(string title, string message, NotificationKind kind)
    {
        Title = title;
        Message = message;
        Kind = kind;
    }
}
