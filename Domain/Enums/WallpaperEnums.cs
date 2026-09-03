namespace NTWallpaper.Domain.Enums;

/// <summary>Windows wallpaper positioning styles (maps to DESKTOP_WALLPAPER_POSITION).</summary>
public enum WallpaperStyle
{
    Center,
    Tile,
    Stretch,
    Fit,
    Fill,
    Span
}

/// <summary>Rotation schedule presets.</summary>
public enum RotationInterval
{
    Disabled,
    Minutes15,
    Minutes30,
    Hour1,
    Hours2,
    Hours4,
    Hours6,
    Hours12,
    Daily,
    Custom,
    SpecificTime
}

public enum ImageType
{
    All,
    Photo,
    Illustration,
    Vector
}

public enum Orientation
{
    All,
    Horizontal,
    Vertical
}

/// <summary>High-level application/operation states exposed to the UI.</summary>
public enum AppState
{
    Idle,
    Searching,
    Downloading,
    Validating,
    Applying,
    Applied,
    Retrying,
    UsingCache,
    Failed,
    Paused
}

public enum TargetKind
{
    Monitor,
    VirtualDesktop,
    LockScreen
}

public enum ThemePreference
{
    System,
    Light,
    Dark
}
