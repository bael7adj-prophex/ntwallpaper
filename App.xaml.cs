using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTWallpaper.Application.Orchestration;
using NTWallpaper.Application.Services;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Infrastructure.Networking;
using NTWallpaper.Infrastructure.Persistence;
using NTWallpaper.Infrastructure.Pixabay;
using NTWallpaper.Infrastructure.Security;
using NTWallpaper.Infrastructure.Windows;
using NTWallpaper.Presentation.ViewModels;
using Serilog;
using Serilog.Extensions.Logging;

namespace NTWallpaper;

public partial class App : Application
{
    private ServiceProvider? _services;
    private NotifyIcon? _trayIcon;
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure logging
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PixabayWallpaper", "logs", "ntwallpaper-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Default paths
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PixabayWallpaper");
        Directory.CreateDirectory(appData);
        var dbPath = SettingsService.DefaultDatabasePath();
        var cacheDir = Path.Combine(appData, "Wallpapers");
        var secretDir = Path.Combine(appData, "secrets");

        var db = new Database(dbPath);
        db.Initialize();

        // DI container
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<ISecureStorage>(_ => new SecureStorage(secretDir));
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IImageRepository, ImageRepository>();
        services.AddSingleton<IGroupRepository, GroupRepository>();
        services.AddSingleton<ITagRepository, TagRepository>();
        services.AddSingleton<IImageProvider, PixabayImageProvider>();
        services.AddSingleton<IImageDownloadService, ImageDownloadService>();
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<IWallpaperService, WallpaperService>();
        services.AddSingleton<ILockScreenService, LockScreenService>();
        services.AddSingleton<IVirtualDesktopService, VirtualDesktopService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IImageRenderingService, ImageRenderingService>();
        services.AddSingleton<IRecommendationService, RecommendationService>();
        services.AddSingleton<ISchedulerService, SchedulerService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<WallpaperOrchestrator>();

        services.AddLogging(b => b.AddSerilog(dispose: true));
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<GroupsViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        _services = services.BuildServiceProvider();
        Services = _services;

        var orchestrator = _services.GetRequiredService<WallpaperOrchestrator>();
        var settings = _services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync(default);

        // Ensure cache directory
        Directory.CreateDirectory(settings.Current.CacheDirectory);

        await orchestrator.InitializeAsync(default);
        orchestrator.Start();

        // Tray icon
        SetupTray(_services);

        // Notifications → tray balloon
        var notifications = _services.GetRequiredService<INotificationService>();
        if (notifications is NotificationService ns)
        {
            ns.NotificationRequested += (_, args) => ShowTrayBalloon(args.Title, args.Message, args.Kind);
        }

        // Show window unless start-minimized
        var startupArgs = Environment.GetCommandLineArgs();
        var startMinimized = settings.Current.StartMinimized && !startupArgs.Any(a => a.Equals("--show", StringComparison.OrdinalIgnoreCase));

        var window = _services.GetRequiredService<MainWindow>();
        MainWindow = window;
        if (!startMinimized)
        {
            window.Show();
        }
        else
        {
            _trayIcon!.ShowBalloonTip(2000, "Pixabay Wallpaper Manager", "Running in the system tray. Double-click the tray icon to open.", ToolTipIcon.Info);
        }
    }

    private void SetupTray(IServiceProvider sp)
    {
        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Pixabay Wallpaper Manager"
        };
        _trayIcon.DoubleClick += (_, __) => ShowMainWindow();

        var menu = new ContextMenuStrip();
        var open = new ToolStripMenuItem("Open") { Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold) };
        open.Click += (_, __) => ShowMainWindow();
        var newWp = new ToolStripMenuItem("New Wallpaper");
        newWp.Click += (_, __) => sp.GetRequiredService<WallpaperOrchestrator>().TriggerGroup(sp.GetRequiredService<ISettingsService>().Current is null ? "" : "");
        // For simplicity, trigger first group
        newWp.Click += async (_, __) =>
        {
            var groups = await sp.GetRequiredService<IGroupRepository>().GetAllAsync(default);
            if (groups.Count > 0) sp.GetRequiredService<WallpaperOrchestrator>().TriggerGroup(groups[0].Id);
        };

        var settings = new ToolStripMenuItem("Settings…");
        settings.Click += (_, __) => { ShowMainWindow(); ((MainViewModel)MainWindow!.DataContext).Navigate("Settings"); };

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, __) => Shutdown();

        menu.Items.AddRange(new ToolStripItem[] { open, new ToolStripSeparator(), newWp, settings, new ToolStripSeparator(), exit });
        _trayIcon.ContextMenuStrip = menu;
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null) return;
        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    private void ShowTrayBalloon(string title, string message, NotificationKind kind)
    {
        if (_trayIcon is null) return;
        var icon = kind switch
        {
            NotificationKind.Error => ToolTipIcon.Error,
            NotificationKind.Warning => ToolTipIcon.Warning,
            _ => ToolTipIcon.Info
        };
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = message;
        _trayIcon.ShowBalloonTip(3000);
        _ = icon; // suppress warning (icon used implicitly via BalloonTipIcon if needed)
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _trayIcon?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
