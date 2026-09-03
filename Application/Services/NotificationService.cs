namespace NTWallpaper.Application.Services;

using NTWallpaper.Domain.Interfaces;

/// <summary>Raises notification requests; the presentation layer (system tray) renders them.</summary>
public class NotificationService : INotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationRequested;

    public void ShowInfo(string title, string message) => Raise(title, message, NotificationKind.Info);
    public void ShowWarning(string title, string message) => Raise(title, message, NotificationKind.Warning);
    public void ShowError(string title, string message) => Raise(title, message, NotificationKind.Error);

    private void Raise(string title, string message, NotificationKind kind)
        => NotificationRequested?.Invoke(this, new NotificationEventArgs(title, message, kind));
}
